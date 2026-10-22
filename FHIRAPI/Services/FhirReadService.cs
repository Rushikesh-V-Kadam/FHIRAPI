using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Mapping;
using FHIRAPI.Models.Ehr;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Services;

/// <summary>
/// Reads ONE resource by type and id: fetches it live from the EHR and converts it to FHIR.
/// Nothing is cached or stored. Used by the read actions (GET /fhir/r4/Patient/{id} and so on) and by searches.
/// (QuestionnaireResponse is not here: it is stored by this API, see QuestionnaireResponseService.)
/// </summary>
public class FhirReadService
{
    /// <summary>Resource types that can be read and searched (listed in the CapabilityStatement).</summary>
    public static readonly string[] SupportedTypes =
    {
        "Patient", "Coverage", "Practitioner", "PractitionerRole", "Organization", "Location",
        "ServiceRequest", "MedicationRequest",
        "Encounter", "Observation", "Condition", "Procedure", "DocumentReference"
    };

    private readonly IEhrClient _ehr;
    private readonly AdministrativeMapper _admin;
    private readonly OrderMapper _orders;
    private readonly ClinicalMapper _clinical;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public FhirReadService(IEhrClient ehr, AdministrativeMapper admin, OrderMapper orders, ClinicalMapper clinical,
        IOptions<FhirServerSettings> server)
    {
        _ehr = ehr;
        _admin = admin;
        _orders = orders;
        _clinical = clinical;
        _server = server.Value;
    }

    /// <summary>
    /// Returns the resource, or null when the EHR does not have it.
    /// Throws FhirException (400) for an unsupported type.
    /// </summary>
    public async Task<Resource?> ReadAsync(string type, string id, CancellationToken ct)
    {
        switch (type)
        {
            case "Patient":
                var patient = await _ehr.GetPatientAsync(id, ct);
                return patient == null ? null : _admin.ToPatient(patient);

            case "Coverage":
                return await ReadCoverageAsync(id, ct);

            case "Practitioner":
                var practitioner = await _ehr.GetPractitionerAsync(id, ct);
                return practitioner == null ? null : _admin.ToPractitioner(practitioner);

            case "PractitionerRole":
                // PractitionerRole ids are "role-{practitionerId}"
                if (!id.StartsWith("role-")) return null;
                var rolePractitioner = await _ehr.GetPractitionerAsync(id["role-".Length..], ct);
                return rolePractitioner == null ? null : _admin.ToPractitionerRole(rolePractitioner);

            case "Organization":
                // CLIENT-EHR: a facility (Organization/facility-{locationId}) is built from the EHR location + its parent organization
                if (FacilityIds.IsFacility(id))
                {
                    return await ReadFacilityAsync(FacilityIds.ToLocationId(id), ct);
                }
                var organization = await _ehr.GetOrganizationAsync(id, ct);
                return organization == null ? null : _admin.ToOrganization(organization);

            case "Location":
                var location = await _ehr.GetLocationAsync(id, ct);
                return location == null ? null : _admin.ToLocation(location);

            case "ServiceRequest":
            case "MedicationRequest":
                return await ReadOrderAsync(type, id, ct);

            case "Encounter":
                var encounter = await _ehr.GetEncounterAsync(id, ct);
                return encounter == null ? null : _clinical.ToEncounter(encounter);

            case "DocumentReference":
                var document = await _ehr.GetDocumentAsync(id, ct);
                return document == null ? null : _clinical.ToDocumentReference(document, _server.BaseUrl);

            case "Binary":
                var content = await _ehr.GetDocumentContentAsync(id, ct);
                if (content == null) return null;
                var binary = ClinicalMapper.ToBinary(content);
                binary.Id = id;
                return binary;

            default:
                throw new FhirException(400, IssueTypes.NotSupported,
                    $"Read of {type} is not supported. Supported: {string.Join(", ", SupportedTypes)}, Binary, QuestionnaireResponse.");
        }
    }

    /// <summary>
    /// An EHR order becomes one FHIR type (depends on orderType). Asking for the wrong type returns null,
    /// e.g. GET ServiceRequest/ord-1-retacrit when ord-1-retacrit is a medication order.
    /// </summary>
    private async Task<Resource?> ReadOrderAsync(string type, string id, CancellationToken ct)
    {
        var order = await _ehr.GetOrderAsync(id, ct);
        if (order == null || OrderMapper.GetResourceType(order.OrderType) != type) return null;

        return _orders.ToFhir(order);
    }

    /// <summary>
    /// The facility Organization of an EHR location: the location (name, address, facility NPI) plus the tax id of
    /// its parent organization. Null when the EHR has no such location. A missing parent organization is not an
    /// error: the facility is then returned without tax id and without partOf.
    /// </summary>
    private async Task<Resource?> ReadFacilityAsync(string locationId, CancellationToken ct)
    {
        EhrLocationDto? location = await _ehr.GetLocationAsync(locationId, ct);
        if (location == null)
        {
            return null;
        }
        EhrOrganizationDto? parent = null;
        if (!string.IsNullOrEmpty(location.OrganizationId))
        {
            parent = await _ehr.GetOrganizationAsync(location.OrganizationId, ct);
        }
        if (parent == null)
        {
            location.OrganizationId = null;
        }
        return _admin.ToFacilityOrganization(location, parent);
    }

    /// <summary>
    /// One Coverage by the EHR's coverage id (unique in the EHR): the EHR returns exactly one insurance for it,
    /// which already names its patient (Coverage.beneficiary). Null when the EHR has no such insurance (404).
    /// </summary>
    // CLIENT-EHR: the EHR sends its own unique coverage id (coverageId of order-sign / DTR / PAS); read directly by it.
    private async Task<Resource?> ReadCoverageAsync(string coverageId, CancellationToken ct)
    {
        EhrInsuranceDto? insurance = await _ehr.GetInsuranceAsync(coverageId, ct);
        if (insurance == null)
        {
            return null;
        }
        return _admin.ToCoverage(insurance);
    }
}
