namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Period: start and/or end date-time as ISO-8601 strings.
/// </summary>
public class Period
{
    public string? Start { get; set; }
    public string? End { get; set; }
}
