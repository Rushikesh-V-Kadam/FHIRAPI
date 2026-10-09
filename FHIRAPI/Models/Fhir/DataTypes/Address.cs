namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Address: postal address.
/// </summary>
public class Address
{
    public List<string> Line { get; set; } = new();
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
