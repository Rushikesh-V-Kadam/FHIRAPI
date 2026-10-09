using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Standard OAuth 2.0 error body returned by the authorize, token and /internal endpoints.
/// </summary>
public class OAuthError
{
    /// <summary>Error code, e.g. invalid_request, invalid_grant, unsupported_grant_type.</summary>
    /// <example>invalid_request</example>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    /// <summary>What went wrong, in plain text.</summary>
    /// <example>subject and patientId are required.</example>
    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    /// <summary>Creates an error.</summary>
    public OAuthError(string error, string? description = null)
    {
        Error = error;
        ErrorDescription = description;
    }
}
