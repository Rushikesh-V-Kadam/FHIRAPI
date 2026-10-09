namespace FHIRAPI.Constants;

/// <summary>
/// HTTP content types used by the API.
/// </summary>
public static class FhirMediaTypes
{
    /// <summary>Content type of every FHIR response (R4).</summary>
    public const string FhirJson = "application/fhir+json";

    /// <summary>Content type of FHIR R5 responses (/fhir/r5): the fhirVersion parameter tells the client the release.</summary>
    public const string FhirJsonR5 = "application/fhir+json; fhirVersion=5.0";

    /// <summary>Plain JSON (internal and OAuth endpoints).</summary>
    public const string Json = "application/json";
}
