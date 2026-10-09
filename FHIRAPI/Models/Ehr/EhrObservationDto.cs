namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Lab result or vital sign from GET api/patients/{patientId}/observations.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrObservationDto
{
    public string ObservationId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? EncounterId { get; set; }
    public string? Category { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CodeSystem { get; set; } = "LOINC";
    public string? Display { get; set; }
    public decimal? ValueNumber { get; set; }
    public string? Unit { get; set; }
    public string? ValueText { get; set; }
    public EhrCode? ValueCode { get; set; }
    public string Status { get; set; } = "final";
    public string? EffectiveDateTime { get; set; }
    public string? IssuedDateTime { get; set; }
}
