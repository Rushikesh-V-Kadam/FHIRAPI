namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Patient from GET api/patients/{patientId}.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrPatientDto
{
    public string PatientId { get; set; } = string.Empty;
    public string? Mrn { get; set; }
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string? Gender { get; set; }
    public string? DateOfBirth { get; set; }
    public EhrAddress? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public EhrCode? Race { get; set; }
    public EhrCode? Ethnicity { get; set; }
    public bool? Active { get; set; }
}
