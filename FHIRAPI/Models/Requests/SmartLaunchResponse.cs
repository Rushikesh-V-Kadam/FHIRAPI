namespace FHIRAPI.Models.Requests;

/// <summary>
/// Response of POST /internal/smart-launches.
/// The Payer Gateway redirects the browser to {dtrAppUrl}?iss={Iss}&amp;launch={LaunchId}.
/// </summary>
public class SmartLaunchResponse
{
    /// <summary>Opaque launch id the DTR app sends back to /fhir/r4/auth/authorize.</summary>
    /// <example>6-jM9cmaBKMd4rFPAl-hQp_D</example>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>FHIR base URL of this API (the SMART "iss").</summary>
    /// <example>http://localhost:5100/fhir/r4</example>
    public string Iss { get; set; } = string.Empty;

    /// <summary>The launch must be used before this time.</summary>
    /// <example>2026-10-07T18:50:40.2202026+00:00</example>
    public DateTimeOffset ExpiresAt { get; set; }
}
