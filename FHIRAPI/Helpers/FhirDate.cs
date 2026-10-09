namespace FHIRAPI.Helpers;

/// <summary>
/// FHIR date-time strings (ISO-8601, UTC).
/// </summary>
public static class FhirDate
{
    /// <summary>Current time, e.g. "2026-09-28T17:05:00Z".</summary>
    public static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
}
