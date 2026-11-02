using FHIRAPI.Helpers;

namespace FHIRAPI.Models.Security;

/// <summary>
/// OAuth authorization code issued by /fhir/r4/auth/authorize and exchanged once at /fhir/r4/auth/token.
/// Only a hash of the code is stored (PA_SMART_AUTH_CODE.CODE_HASH).
/// </summary>
public class AuthorizationCode
{
    /// <summary>The code sent to the app. Never stored as is.</summary>
    public string Code { get; set; } = RandomIds.New(24);

    /// <summary>The app the code was issued to; the token request must send the same client_id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The token request must send the same redirect_uri.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Scopes granted (space separated).</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>PKCE code_challenge (S256) from authorize; the token request's code_verifier must match it.</summary>
    public string CodeChallenge { get; set; } = string.Empty;

    /// <summary>The launch this code was issued for.</summary>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>Codes are short-lived (2 minutes).</summary>
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(2);
}
