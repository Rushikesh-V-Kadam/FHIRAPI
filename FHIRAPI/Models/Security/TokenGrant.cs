namespace FHIRAPI.Models.Security;

/// <summary>
/// What an access token allows. Stored against a hash of the token by the token service (PA_ACCESS_TOKEN).
/// </summary>
public class TokenGrant
{
    /// <summary>Who the token was issued to: a payer name (from /internal/access-tokens) or a SMART client id (DTR app).</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>SMART scopes, e.g. "patient/*.rs".</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The only patient this token may read or write. Null = no patient limit.</summary>
    public string? PatientId { get; set; }

    /// <summary>FHIR base URL the token is for (SMART "aud"). Requests to another server are rejected.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>After this time the token is rejected.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>True while the token has not expired.</summary>
    public bool IsValid => ExpiresAt > DateTimeOffset.UtcNow;
}
