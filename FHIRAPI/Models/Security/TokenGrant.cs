namespace FHIRAPI.Models.Security;

/// <summary>
/// What an access token allows. Stored against the token string by the token service.
/// </summary>
public class TokenGrant
{
    /// <summary>Who the token was issued to: a payer id (from /internal/access-tokens) or a SMART client id (DTR app).</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>SMART scopes, e.g. "patient/*.rs".</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The only patient this token may read or write. Null = no patient limit.</summary>
    public string? PatientId { get; set; }

    /// <summary>After this time the token is rejected.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>True while the token has not expired.</summary>
    public bool IsValid => ExpiresAt > DateTimeOffset.UtcNow;
}
