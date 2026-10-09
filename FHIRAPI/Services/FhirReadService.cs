using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Mapping;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Services;

/// <summary>
/// Reads ONE resource by type and id: fetches it live from the EHR and converts it to FHIR.
/// Nothing is cached or stored. Used by GET /fhir/r4/{type}/{id} and by searches.
/// (QuestionnaireResponse is not here: it is stored by this API, see QuestionnaireResponseService.)
/// </summary>
public class FhirReadService
{
    /// <summary>Resource types that can be read and searched (listed in the CapabilityStatement).</summary>
    public static readonly string[] SupportedTypes =
    {
        "Patient", "Coverage", "Practitioner", "PractitionerRole", "Organization", "Location",
        "ServiceRequest", "DeviceRequest", "MedicationRequest", "Appointment",
        "Encounter", "Observation", "Condition", "Procedure", "DocumentReference"
    };

    private readonly IEhrClient _ehr;
    private readonly AdministrativeMapper _admin;
    private readonly OrderMapper _orders;
    private readonly ClinicalMapper _clinical;
    private readonly CoveragePatientIndex _coverageIndex;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public FhirReadService(IEhrClient ehr, AdministrativeMapper admin, OrderMapper orders, ClinicalMapper clinical,
        CoveragePatientIndex coverageIndex, IOptions<FhirServerSettings> server)
    {
        _ehr = ehr;
        _admin = admin;
        _orders = orders;
        _clinical = clinical;
        _coverageIndex = coverageIndex;
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
                var organization = await _ehr.GetOrganizationAsync(id, ct);
                return organization == null ? null : _admin.ToOrganization(organization);

            case "Location":
                var location = await _ehr.GetLocationAsync(id, ct);
                return location == null ? null : _admin.ToLocation(location);

            case "ServiceRequest":
            case "DeviceRequest":
            case "MedicationRequest":
                return await ReadOrderAsync(type, id, ct);

            case "Appointment":
                var appointment = await _ehr.GetAppointmentAsync(id, ct);
                return appointment == null ? null : _orders.ToAppointment(appointment);

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
    /// e.g. GET ServiceRequest/ord-cpap when ord-cpap is a device order.
    /// </summary>
    private async Task<Resource?> ReadOrderAsync(string type, string id, CancellationToken ct)
    {
        var order = await _ehr.GetOrderAsync(id, ct);
        if (order == null || OrderMapper.GetResourceType(order.OrderType) != type) return null;

        _coverageIndex.Remember(order.CoverageId, order.PatientId);
        return _orders.ToFhir(order);
    }

    /// <summary>
    /// The EHR can only list a patient's insurances, so the patient of the coverage must be known first
    /// (see CoveragePatientIndex).
    /// </summary>
    private async Task<Resource?> ReadCoverageAsync(string coverageId, CancellationToken ct)
    {
        var patientId = _coverageIndex.FindPatient(coverageId);
        if (patientId == null) return null;

        var insurances = await _ehr.GetInsurancesAsync(patientId, null, ct);
        var insurance = insurances.FirstOrDefault(i => i.CoverageId == coverageId);
        return insurance == null ? null : _admin.ToCoverage(insurance);
    }
}
