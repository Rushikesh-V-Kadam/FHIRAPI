namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Postal address as the EHR sends it.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrAddress
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
