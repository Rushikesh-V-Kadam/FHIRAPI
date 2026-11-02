namespace FHIRAPI.Configuration;

/// <summary>
/// Keycloak, the authorization server for SMART App Launch (appsettings section "Keycloak").
/// Keycloak signs the clinician in (through Azure AD), issues the access tokens and puts the launch context next to them
/// (the SMART launch add-on, E:\FHIR\KeycloakSmartLaunch, reads it from GET /internal/smart-launches/{launchId}/context).
/// This API only checks the tokens: signature (Keycloak's public keys), issuer, audience, expiry, then SMART scopes.
/// </summary>
public class KeycloakSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>
    /// The realm address, exactly as Keycloak writes it in the "iss" claim of its tokens,
    /// e.g. https://keycloak.provider.example/realms/ehr. Empty = Keycloak is not set up (tokens cannot be checked,
    /// .well-known/smart-configuration answers 503).
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Optional. Address this API uses to read Keycloak's OpenID configuration and keys, when it differs from the public
    /// Authority (e.g. Keycloak reached over the internal network). Empty = {Authority}/.well-known/openid-configuration.
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Accepted "aud" values of an access token. Empty = the FHIR base URL (FhirServer:PublicBaseUrl), which is the
    /// SMART "aud" an app sends. In Keycloak an Audience mapper adds this value to the tokens.
    /// </summary>
    public List<string> Audiences { get; set; } = new List<string>();

    /// <summary>true = the OpenID configuration and keys must be read over https. Set false only for local testing.</summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>Allowed clock difference between Keycloak and this server when checking expiry, in seconds.</summary>
    public int ClockSkewSeconds { get; set; } = 60;

    /// <summary>True when Keycloak is set up.</summary>
    public bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(Authority);
    }

    /// <summary>The realm address without a trailing slash (the expected "iss").</summary>
    public string GetIssuer()
    {
        return (Authority ?? string.Empty).Trim().TrimEnd('/');
    }

    /// <summary>Where the OpenID configuration (endpoints, jwks_uri) is read.</summary>
    public string GetMetadataAddress()
    {
        if (!string.IsNullOrWhiteSpace(MetadataAddress))
        {
            return MetadataAddress.Trim();
        }
        return GetIssuer() + "/.well-known/openid-configuration";
    }

    /// <summary>Keycloak's authorization endpoint (standard Keycloak path of the realm).</summary>
    public string GetAuthorizationEndpoint()
    {
        return GetIssuer() + "/protocol/openid-connect/auth";
    }

    /// <summary>Keycloak's token endpoint (standard Keycloak path of the realm).</summary>
    public string GetTokenEndpoint()
    {
        return GetIssuer() + "/protocol/openid-connect/token";
    }

    /// <summary>Keycloak's public signing keys (standard Keycloak path of the realm).</summary>
    public string GetJwksUri()
    {
        return GetIssuer() + "/protocol/openid-connect/certs";
    }

    /// <summary>Keycloak's token revocation endpoint (standard Keycloak path of the realm).</summary>
    public string GetRevocationEndpoint()
    {
        return GetIssuer() + "/protocol/openid-connect/revoke";
    }

    /// <summary>Keycloak's token introspection endpoint (standard Keycloak path of the realm).</summary>
    public string GetIntrospectionEndpoint()
    {
        return GetIssuer() + "/protocol/openid-connect/token/introspect";
    }
}
