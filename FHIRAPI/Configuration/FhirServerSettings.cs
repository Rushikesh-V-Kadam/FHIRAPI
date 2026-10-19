namespace FHIRAPI.Configuration;

/// <summary>
/// Settings for the FHIR endpoints (appsettings section "FhirServer").
/// </summary>
public class FhirServerSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "FhirServer";

    /// <summary>
    /// Public base URL of the FHIR API, e.g. https://fhir.provider.example/fhir/r4.
    /// Used in Bundle fullUrls, Binary links and the SMART configuration.
    /// </summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5100/fhir/r4";


    /// <summary>Maximum number of matches returned in one search Bundle.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>Base URL without a trailing slash.</summary>
    public string BaseUrl => PublicBaseUrl.TrimEnd('/');
}
