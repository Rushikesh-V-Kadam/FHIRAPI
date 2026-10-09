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
    Task<EhrPractitionerDto?> GetPractitionerAsync(string practitionerId, CancellationToken ct);
    Task<EhrOrganizationDto?> GetOrganizationAsync(string organizationId, CancellationToken ct);
    Task<EhrLocationDto?> GetLocationAsync(string locationId, CancellationToken ct);
    Task<EhrOrderDto?> GetOrderAsync(string orderId, CancellationToken ct);
    Task<EhrAppointmentDto?> GetAppointmentAsync(string appointmentId, CancellationToken ct);
    Task<EhrEncounterDto?> GetEncounterAsync(string encounterId, CancellationToken ct);
    Task<List<EhrEncounterDto>> GetEncountersAsync(string patientId, string? from, CancellationToken ct);
    Task<List<EhrObservationDto>> GetObservationsAsync(string patientId, string? codes, string? category, string? from, CancellationToken ct);
    Task<List<EhrConditionDto>> GetConditionsAsync(string patientId, string? codes, string? status, CancellationToken ct);
    Task<List<EhrProcedureDto>> GetProceduresAsync(string patientId, string? codes, string? from, CancellationToken ct);
    Task<List<EhrDocumentDto>> GetDocumentsAsync(string patientId, string? typeCode, CancellationToken ct);
    Task<EhrDocumentDto?> GetDocumentAsync(string documentId, CancellationToken ct);
    Task<EhrDocumentContentDto?> GetDocumentContentAsync(string documentId, CancellationToken ct);

    /// <summary>Writes a DTR form to the EHR (POST when new, PUT when ResponseId is set). Returns the EHR's copy.</summary>
    Task<EhrQuestionnaireResponseDto> SaveQuestionnaireResponseAsync(EhrQuestionnaireResponseDto form, CancellationToken ct);
}
