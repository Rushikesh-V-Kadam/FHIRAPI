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

    /// <summary>
    /// Public base URL of the FHIR R5 view of the same data, e.g. https://fhir.provider.example/fhir/r5.
    /// Used in R5 responses (fullUrls, Binary links), the R5 SMART configuration and R5 SMART launches.
    /// </summary>
    public string PublicBaseUrlR5 { get; set; } = "http://localhost:5100/fhir/r5";

    /// <summary>Maximum number of matches returned in one search Bundle.</summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>R4 base URL without a trailing slash.</summary>
    public string BaseUrl => PublicBaseUrl.TrimEnd('/');

    /// <summary>R5 base URL without a trailing slash.</summary>
    public string BaseUrlR5 => PublicBaseUrlR5.TrimEnd('/');
}
