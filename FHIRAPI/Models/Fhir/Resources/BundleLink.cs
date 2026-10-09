namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Link on a Bundle, e.g. relation "self" with the search URL.
/// </summary>
public class BundleLink
{
    public string Relation { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}
