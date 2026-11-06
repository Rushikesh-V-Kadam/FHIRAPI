using FHIRAPI.Models.Ehr;

namespace FHIRAPI.Services;

/// <summary>
/// Calls the EHR REST APIs. "Get one" methods return null when the EHR answers 404.
/// Any other failure throws EhrException (returned to the caller as 502).
/// </summary>
public interface IEhrClient
{
    Task<EhrPatientDto?> GetPatientAsync(string patientId, CancellationToken ct);
    Task<List<EhrInsuranceDto>> GetInsurancesAsync(string patientId, string? status, CancellationToken ct);
    /// <summary>One insurance by its EHR coverage id (unique in the EHR). Null when the EHR answers 404.</summary>
    Task<EhrInsuranceDto?> GetInsuranceAsync(string coverageId, CancellationToken ct);
    Task<EhrPractitionerDto?> GetPractitionerAsync(string practitionerId, CancellationToken ct);
    Task<EhrOrganizationDto?> GetOrganizationAsync(string organizationId, CancellationToken ct);
    Task<EhrLocationDto?> GetLocationAsync(string locationId, CancellationToken ct);
    Task<EhrOrderDto?> GetOrderAsync(string orderId, CancellationToken ct);
    Task<EhrEncounterDto?> GetEncounterAsync(string encounterId, CancellationToken ct);
    Task<List<EhrEncounterDto>> GetEncountersAsync(string patientId, string? from, CancellationToken ct);
    /// <summary>All lab results of a patient (the client's API has no filters). Empty list for an unknown patient.</summary>
    Task<List<EhrObservationDto>> GetObservationsAsync(string patientId, CancellationToken ct);
    /// <summary>All diagnoses of a patient (the client's API has no filters). Empty list for an unknown patient.</summary>
    Task<List<EhrConditionDto>> GetConditionsAsync(string patientId, CancellationToken ct);
    /// <summary>All procedures of a patient (the client's API has no filters). Empty list for an unknown patient.</summary>
    Task<List<EhrProcedureDto>> GetProceduresAsync(string patientId, CancellationToken ct);
    Task<List<EhrDocumentDto>> GetDocumentsAsync(string patientId, string? typeCode, CancellationToken ct);
    Task<EhrDocumentDto?> GetDocumentAsync(string documentId, CancellationToken ct);
    Task<EhrDocumentContentDto?> GetDocumentContentAsync(string documentId, CancellationToken ct);

    /// <summary>Writes a DTR form to the EHR (POST when new, PUT when ResponseId is set). Returns the EHR's copy.</summary>
    Task<EhrQuestionnaireResponseDto> SaveQuestionnaireResponseAsync(EhrQuestionnaireResponseDto form, CancellationToken ct);
}
