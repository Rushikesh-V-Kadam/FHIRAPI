namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Encounter (visit) from GET api/encounters/{encounterId} or api/patients/{patientId}/encounters.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrEncounterDto
{
    public string EncounterId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = "finished";
    public string? EncounterClass { get; set; }
    public string? TypeCode { get; set; }
    public string? TypeCodeSystem { get; set; }
    public string? TypeDisplay { get; set; }
    public string? StartDateTime { get; set; }
    public string? EndDateTime { get; set; }
    public string? PractitionerId { get; set; }
    public string? OrganizationId { get; set; }
    public string? LocationId { get; set; }
    public List<EhrCode> ReasonDiagnoses { get; set; } = new();
    public string? DischargeDisposition { get; set; }
}
