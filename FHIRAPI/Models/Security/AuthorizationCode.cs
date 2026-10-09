using FHIRAPI.Helpers;

namespace FHIRAPI.Models.Security;

/// <summary>
/// OAuth authorization code issued by /fhir/r4/auth/authorize and exchanged once at /fhir/r4/auth/token.
/// </summary>
public class AuthorizationCode
{
    public string Code { get; set; } = RandomIds.New(24);
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The token request must send the same redirect_uri.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;


    /// <summary>The launch this code was issued for.</summary>
    public SmartLaunch Launch { get; set; } = new();

    /// <summary>Codes are short-lived (2 minutes).</summary>
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(2);
}
