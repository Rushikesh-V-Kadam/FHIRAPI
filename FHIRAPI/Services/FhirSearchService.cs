using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Mapping;
using FHIRAPI.Models.Fhir;
using FHIRAPI.Models.Requests;

namespace FHIRAPI.Services;

/// <summary>
/// Runs GET /fhir/r4/{type}?... searches against the EHR and returns a FHIR "searchset" Bundle.
///
/// Required parameters per type (the EHR can only look data up this way):
///   Patient, Practitioner, PractitionerRole, Organization, Location,
///   ServiceRequest, DeviceRequest, MedicationRequest, Appointment ....... _id
///   Coverage, Observation, Condition, Procedure, DocumentReference ....... patient
///   Encounter ........................................................... _id or patient
/// A search without its required parameter returns 400 with an OperationOutcome that says what is missing.
/// </summary>
public class FhirSearchService
{
    private readonly IEhrClient _ehr;
    private readonly FhirReadService _reader;
    private readonly AdministrativeMapper _admin;
    private readonly ClinicalMapper _clinical;
    private readonly CodeSystemMapper _codes;
    private readonly CoveragePatientIndex _coverageIndex;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public FhirSearchService(IEhrClient ehr, FhirReadService reader, AdministrativeMapper admin, ClinicalMapper clinical,
        CodeSystemMapper codes, CoveragePatientIndex coverageIndex, IOptions<FhirServerSettings> server)
    {
        _ehr = ehr;
        _reader = reader;
        _admin = admin;
        _clinical = clinical;
        _codes = codes;
        _coverageIndex = coverageIndex;
        _server = server.Value;
    }

    /// <summary>Runs the search, adds _include resources and builds the Bundle.</summary>
    public async Task<Bundle> SearchAsync(string type, FhirSearchParameters search, CancellationToken ct)
    {
        var matches = await FindMatchesAsync(type, search, ct);
        var included = await FindIncludesAsync(matches, search.Include, ct);
        return BuildBundle(type, matches, included);
    }

    // ------------------------------------------------------------------ step 1: matches

    /// <summary>Asks the EHR for the matching records and converts them.</summary>
    private async Task<List<Resource>> FindMatchesAsync(string type, FhirSearchParameters search, CancellationToken ct)
    {
        var results = new List<Resource>();

        switch (type)
        {
            // ---- searched by id: read each id
            case "Patient":
            case "Practitioner":
            case "PractitionerRole":
            case "Organization":
            case "Location":
            case "ServiceRequest":
            case "DeviceRequest":
            case "MedicationRequest":
            case "Appointment":
                foreach (var id in RequireIds(type, search))
                {
                    var resource = await _reader.ReadAsync(type, id, ct);
                    if (resource != null) results.Add(resource);
                }
                break;

            // ---- searched by id or by patient
            case "Encounter":
                if (search.GetIds().Count > 0)
                {
                    foreach (var id in search.GetIds())
                    {
                        var encounter = await _reader.ReadAsync(type, id, ct);
                        if (encounter != null) results.Add(encounter);
                    }
                }
                else
                {
                    var encounters = await _ehr.GetEncountersAsync(RequirePatient(type, search), search.GetDateFrom(), ct);
                    results.AddRange(encounters.Select(_clinical.ToEncounter));
                }
                break;

            // ---- searched by patient
            case "Coverage":
                var insurances = await _ehr.GetInsurancesAsync(RequirePatient(type, search), search.Status, ct);
                foreach (var insurance in insurances)
                {
                    _coverageIndex.Remember(insurance.CoverageId, insurance.PatientId);
                    results.Add(_admin.ToCoverage(insurance));
                }
                break;

            case "Observation":
                var observations = await _ehr.GetObservationsAsync(RequirePatient(type, search),
                    _codes.ToEhrCodeList(search.Code), search.Category, search.GetDateFrom(), ct);
                results.AddRange(observations.Select(_clinical.ToObservation));
                break;

            case "Condition":
                var conditions = await _ehr.GetConditionsAsync(RequirePatient(type, search),
                    _codes.ToEhrCodeList(search.Code), search.ClinicalStatus, ct);
                results.AddRange(conditions.Select(_clinical.ToCondition));
                break;

            case "Procedure":
                var procedures = await _ehr.GetProceduresAsync(RequirePatient(type, search),
                    _codes.ToEhrCodeList(search.Code), search.GetDateFrom(), ct);
                results.AddRange(procedures.Select(_clinical.ToProcedure));
                break;

            case "DocumentReference":
                // type=http://loinc.org|18748-4 -> the EHR wants just the code "18748-4"
                var typeCode = search.Type?.Split('|').Last();
                var documents = await _ehr.GetDocumentsAsync(RequirePatient(type, search), typeCode, ct);
                results.AddRange(documents.Select(d => _clinical.ToDocumentReference(d, _server.BaseUrl)));
                break;

            default:
                throw new FhirException(400, IssueTypes.NotSupported,
                    $"Search of {type} is not supported. Supported: {string.Join(", ", FhirReadService.SupportedTypes)}, QuestionnaireResponse.");
        }

        return results;
    }

    // ------------------------------------------------------------------ step 2: _include

    /// <summary>
    /// Adds the resources named by _include values of the form "SourceType:parameter[:TargetType]", e.g.
    /// "DeviceRequest:requester" or "MedicationRequest:requester:PractitionerRole".
    /// CRD prefetch templates use this to get an order and its practitioner, patient and coverage in one call.
    /// </summary>
    private async Task<List<Resource>> FindIncludesAsync(List<Resource> matches, List<string> includes, CancellationToken ct)
    {
        var included = new List<Resource>();

        foreach (var include in includes)
        {
            var parts = include.Split(':');
            if (parts.Length < 2) continue;
            var sourceType = parts[0];
            var parameter = parts[1];
            var targetType = parts.Length > 2 ? parts[2] : null;

            foreach (var match in matches.Where(m => m.ResourceType == sourceType))
            {
                foreach (var reference in ReferenceFinder.Find(match, parameter))
                {
                    var refType = reference.GetResourceType();
                    var refId = reference.GetId();
                    if (refType == null || refId == null) continue;

                    // "...:requester:PractitionerRole" asks for the role of the practitioner
                    if (targetType == "PractitionerRole" && refType == "Practitioner")
                    {
                        refType = "PractitionerRole";
                        refId = $"role-{refId}";
                    }

                    await AddOnceAsync(included, matches, refType, refId, ct);

                    // A PractitionerRole also brings its Practitioner (the NPI is on the Practitioner)
                    if (refType == "PractitionerRole" && refId.StartsWith("role-"))
                        await AddOnceAsync(included, matches, "Practitioner", refId["role-".Length..], ct);
                }
            }
        }
        return included;
    }

    /// <summary>Reads a referenced resource and adds it, unless it is already in the Bundle.</summary>
    private async Task AddOnceAsync(List<Resource> included, List<Resource> matches, string type, string id, CancellationToken ct)
    {
        var alreadyThere = included.Concat(matches).Any(r => r.ResourceType == type && r.Id == id);
        if (alreadyThere || !FhirReadService.SupportedTypes.Contains(type)) return;

        var resource = await _reader.ReadAsync(type, id, ct);
        if (resource != null) included.Add(resource);
    }

    // ------------------------------------------------------------------ step 3: Bundle

    /// <summary>Builds the searchset Bundle: matches first (mode "match"), then includes (mode "include").</summary>
    private Bundle BuildBundle(string type, List<Resource> matches, List<Resource> included)
    {
        var bundle = new Bundle
        {
            Id = Guid.NewGuid().ToString("N"),
            Timestamp = FhirDate.Now(),
            Total = matches.Count
        };
        bundle.Link.Add(new BundleLink { Relation = "self", Url = $"{_server.BaseUrl}/{type}" });

        foreach (var resource in matches.Take(_server.MaxPageSize))
            bundle.Entry.Add(NewEntry(resource, "match"));
        foreach (var resource in included)
            bundle.Entry.Add(NewEntry(resource, "include"));

        return bundle;
    }

    /// <summary>One Bundle entry with its absolute fullUrl.</summary>
    private BundleEntry NewEntry(Resource resource, string mode) => new()
    {
        FullUrl = $"{_server.BaseUrl}/{resource.ResourceType}/{resource.Id}",
        Resource = resource,
        Search = new BundleEntrySearch { Mode = mode }
    };

    // ------------------------------------------------------------------ parameter checks

    /// <summary>The _id values, or 400 when none were given.</summary>
    private static List<string> RequireIds(string type, FhirSearchParameters search)
    {
        var ids = search.GetIds();
        if (ids.Count == 0)
            throw new FhirException(400, IssueTypes.Required, $"{type} search requires the _id parameter, e.g. {type}?_id=123.");
        return ids;
    }

    /// <summary>The patient id, or 400 when patient/subject/beneficiary was not given.</summary>
    private static string RequirePatient(string type, FhirSearchParameters search) =>
        search.GetPatientId()
        ?? throw new FhirException(400, IssueTypes.Required, $"{type} search requires the patient parameter, e.g. {type}?patient=pat-1.");
}
