namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Location from GET api/locations/{locationId}.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrLocationDto
{
    public string LocationId { get; set; } = string.Empty;
    public string? Name { get; set; }
    /// <summary>NPI of the facility at this location (the client keeps the facility NPI on the location).</summary>
    public string? Npi { get; set; }
    public string? PlaceOfServiceCode { get; set; }
    public EhrAddress? Address { get; set; }
    public string? OrganizationId { get; set; }
}
