namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Diagnosis from GET api/patients/{patientId}/conditions.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrConditionDto
{
    public string ConditionId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CodeSystem { get; set; } = "ICD10CM";
    public string? Display { get; set; }
    public string? ClinicalStatus { get; set; }
    public string? VerificationStatus { get; set; }
    public string? Category { get; set; }
    public string? OnsetDate { get; set; }
    public string? RecordedDate { get; set; }
    public string? EncounterId { get; set; }
}
