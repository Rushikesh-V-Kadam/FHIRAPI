namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Organization from GET api/organizations/{organizationId}.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrOrganizationDto
{
    public string OrganizationId { get; set; } = string.Empty;
    public string? Npi { get; set; }
    public string? TaxId { get; set; }
    public string? Name { get; set; }
    public EhrAddress? Address { get; set; }
    public string? Phone { get; set; }
    public bool? Active { get; set; }
}
