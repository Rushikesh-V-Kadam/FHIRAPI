namespace FHIRAPI.Configuration;

/// <summary>
/// Values returned by GET /fhir/r4/.well-known/smart-configuration (appsettings section "SmartConfiguration").
/// They describe the authorization server that SMART apps (payers' DTR apps) must use with this FHIR API.
/// The client's IdP is Azure Entra ID, so the endpoint URLs are Entra URLs of the client's tenant.
/// Every value comes from settings: nothing is hard-coded, so another IdP (or a SMART proxy in front of Entra)
/// only needs new settings, not code changes.
/// Lists default to empty on purpose: the .NET configuration binder adds appsettings items to a pre-filled list
/// instead of replacing it.
/// </summary>
public class SmartConfigurationSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "SmartConfiguration";

    /// <summary>
    /// "issuer": the IdP's issuer URL (the "iss" claim of its id_tokens).
    /// Entra ID: https://login.microsoftonline.com/{tenant-id}/v2.0.
    /// Required when Capabilities contains "sso-openid-connect".
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// "jwks_uri": URL of the IdP's public signing keys.
    /// Entra ID: https://login.microsoftonline.com/{tenant-id}/discovery/v2.0/keys.
    /// Required when Capabilities contains "sso-openid-connect".
    /// </summary>
    public string JwksUri { get; set; } = string.Empty;

    /// <summary>
    /// "authorization_endpoint": where the SMART app sends the user to log in and get a code.
    /// Entra ID: https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize.
    /// Required when Capabilities contains "launch-ehr" or "launch-standalone".
    /// </summary>
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// "token_endpoint": where the SMART app swaps the code for an access token. Always required.
    /// Entra ID: https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>"introspection_endpoint" (optional). Left out of the response when empty.</summary>
    public string IntrospectionEndpoint { get; set; } = string.Empty;

    /// <summary>"revocation_endpoint" (optional). Left out of the response when empty.</summary>
    public string RevocationEndpoint { get; set; } = string.Empty;

    /// <summary>"registration_endpoint" (optional, dynamic client registration). Left out of the response when empty.</summary>
    public string RegistrationEndpoint { get; set; } = string.Empty;

    /// <summary>"management_endpoint" (optional, page where users manage app authorizations). Left out when empty.</summary>
    public string ManagementEndpoint { get; set; } = string.Empty;

    /// <summary>"grant_types_supported", e.g. ["authorization_code"]. Always required (at least one value).</summary>
    public List<string> GrantTypesSupported { get; set; } = new List<string>();

    /// <summary>"code_challenge_methods_supported" (PKCE). Always required and must contain "S256".</summary>
    public List<string> CodeChallengeMethodsSupported { get; set; } = new List<string>();

    /// <summary>"capabilities": SMART features supported, e.g. "launch-ehr", "client-public". Always required.</summary>
    public List<string> Capabilities { get; set; } = new List<string>();

    /// <summary>"token_endpoint_auth_methods_supported" (optional), e.g. "client_secret_post", "private_key_jwt".</summary>
    public List<string> TokenEndpointAuthMethodsSupported { get; set; } = new List<string>();

    /// <summary>"scopes_supported" (optional), e.g. "launch", "openid", "patient/Patient.rs".</summary>
    public List<string> ScopesSupported { get; set; } = new List<string>();

    /// <summary>"response_types_supported" (optional), e.g. ["code"].</summary>
    public List<string> ResponseTypesSupported { get; set; } = new List<string>();
}
