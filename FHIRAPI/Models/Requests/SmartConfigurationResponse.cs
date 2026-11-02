using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Body of GET /fhir/r4/.well-known/smart-configuration (SMART App Launch discovery document).
/// The endpoints are this API's (Security:TokenProvider = "BuiltIn") or Keycloak's ("Keycloak").
/// </summary>
public class SmartConfigurationResponse
{
    /// <summary>BuiltIn: the FHIR base URL. Keycloak: the realm address (the "iss" of Keycloak's tokens and id_tokens).</summary>
    /// <example>https://keycloak.provider.example/realms/ehr</example>
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Keycloak only: Keycloak's public signing keys. Left out with BuiltIn.</summary>
    /// <example>https://keycloak.provider.example/realms/ehr/protocol/openid-connect/certs</example>
    [JsonPropertyName("jwks_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JwksUri { get; set; }

    /// <summary>Where the app signs the clinician in and gets a code (this API's auth/authorize, or Keycloak).</summary>
    /// <example>https://keycloak.provider.example/realms/ehr/protocol/openid-connect/auth</example>
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>Where the app exchanges the code for the token and the launch context (this API's auth/token, or Keycloak).</summary>
    /// <example>https://keycloak.provider.example/realms/ehr/protocol/openid-connect/token</example>
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>Keycloak only: where the app can revoke a token. Left out with BuiltIn.</summary>
    /// <example>https://keycloak.provider.example/realms/ehr/protocol/openid-connect/revoke</example>
    [JsonPropertyName("revocation_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RevocationEndpoint { get; set; }

    /// <summary>Keycloak only: the token introspection endpoint. Left out with BuiltIn.</summary>
    /// <example>https://keycloak.provider.example/realms/ehr/protocol/openid-connect/token/introspect</example>
    [JsonPropertyName("introspection_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IntrospectionEndpoint { get; set; }

    /// <summary>BuiltIn: authorization_code. Keycloak: also client_credentials (CRD fhirAuthorization tokens of the Payer Gateway).</summary>
    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; } = { "authorization_code", "client_credentials" };

    /// <summary>BuiltIn: "none" (public apps with PKCE). Keycloak: also confidential apps (client secret or signed JWT).</summary>
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; } = { "none", "client_secret_basic", "client_secret_post", "private_key_jwt" };

    /// <summary>PKCE method: only S256 (set "PKCE Method = S256" on each DTR app client in Keycloak).</summary>
    [JsonPropertyName("code_challenge_methods_supported")]
    public string[] CodeChallengeMethodsSupported { get; set; } = { "S256" };

    /// <summary>Only "code".</summary>
    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; } = { "code" };

    /// <summary>
    /// Scopes an app may ask for (SMART v2 "cruds" and v1 "read"/"write"). With Keycloak each one is a client scope in
    /// the realm (E:\FHIR\KeycloakSmartLaunch\README.md).
    /// </summary>
    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; set; } =
        { "openid", "fhirUser", "launch", "patient/*.rs", "user/*.rs", "patient/QuestionnaireResponse.cu", "patient/*.read" };

    /// <summary>SMART capabilities of this server (set per TokenProvider by SmartConfigurationController).</summary>
    [JsonPropertyName("capabilities")]
    public string[] Capabilities { get; set; } =
    { "launch-ehr", "authorize-post", "client-public", "client-confidential-symmetric", "client-confidential-asymmetric",
      "sso-openid-connect", "context-ehr-patient", "context-ehr-encounter",
      "permission-v1", "permission-v2", "permission-patient", "permission-user" };
}
