namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Quantity: a measured amount, e.g. 32 "events/h" or 30 "days".
/// </summary>
public class Quantity
{
    public decimal? Value { get; set; }
    public string? Unit { get; set; }
    public string? System { get; set; }
    public string? Code { get; set; }
}
