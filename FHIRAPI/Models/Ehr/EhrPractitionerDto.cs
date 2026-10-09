namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Practitioner from GET api/practitioners/{practitionerId}.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrPractitionerDto
{
    public string PractitionerId { get; set; } = string.Empty;
    public string? Npi { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Prefix { get; set; }
    public EhrCode? Specialty { get; set; }
    public string? OrganizationId { get; set; }
    public string? Phone { get; set; }
    public bool? Active { get; set; }
}
