namespace FHIRAPI.Constants;

/// <summary>
/// HTTP content types used by the API.
/// </summary>
public static class FhirMediaTypes
{
    /// <summary>Content type of every FHIR response (R4).</summary>
    public const string FhirJson = "application/fhir+json";

    /// <summary>Plain JSON (internal and OAuth endpoints).</summary>
    public const string Json = "application/json";
}
