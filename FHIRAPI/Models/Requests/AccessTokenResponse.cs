using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Response of POST /internal/access-tokens.
/// Same shape as the CDS Hooks "fhirAuthorization" object, so the Payer Gateway can pass it to the payer unchanged.
/// </summary>
public class AccessTokenResponse
{
    /// <summary>The token (a random value).</summary>
    /// <example>xhpE4XN-Z8MoI7ODwmf6JEQDnr6LFW60GmyUhVJCycw</example>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Always "Bearer".</summary>
    /// <example>Bearer</example>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Seconds until the token expires.</summary>
    /// <example>300</example>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>The scopes the token was given.</summary>
    /// <example>patient/Patient.rs patient/Coverage.rs patient/Observation.rs</example>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>Who the token was issued to (the payer id).</summary>
    /// <example>PAYER001</example>
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    /// <summary>The patient the token is limited to.</summary>
    /// <example>pat-1</example>
    [JsonPropertyName("patient")]
    public string Patient { get; set; } = string.Empty;
}
