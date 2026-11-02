using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Checks the Bearer tokens sent to /fhir/r4. The tokens are issued by Keycloak (KeycloakTokenValidator).
/// </summary>
public interface IAccessTokenValidator
{
    /// <summary>What the token allows, or null when the token is not valid (bad signature, expired, wrong issuer or audience).</summary>
    Task<TokenGrant?> ValidateAsync(string token, CancellationToken ct);
}
