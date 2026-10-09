namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Past procedure from GET api/patients/{patientId}/procedures.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrProcedureDto
{
    public string ProcedureId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string CodeSystem { get; set; } = "CPT";
    public string? Display { get; set; }
    public string Status { get; set; } = "completed";
    public string? PerformedDateTime { get; set; }
    public string? PerformerPractitionerId { get; set; }
    public string? EncounterId { get; set; }
}
