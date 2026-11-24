using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Smart;

/// <summary>
/// Body of GET /fhir/r4/.well-known/smart-configuration, as defined by the HL7 SMART App Launch 2.x specification
/// (https://hl7.org/fhir/smart-app-launch/conformance.html). Names are snake_case as the specification requires.
/// Optional values that are not configured are left out of the JSON (not sent as null).
/// </summary>
public class SmartConfiguration
{
    /// <summary>Issuer URL of the IdP. Sent when "sso-openid-connect" is a capability.</summary>
    [JsonPropertyName("issuer")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Issuer { get; set; }

    /// <summary>URL of the IdP's public signing keys. Sent when "sso-openid-connect" is a capability.</summary>
    [JsonPropertyName("jwks_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? JwksUri { get; set; }

    /// <summary>OAuth 2.0 authorize URL (the user logs in here and the app gets a code).</summary>
    [JsonPropertyName("authorization_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>OAuth 2.0 token URL (the app swaps the code for an access token here). Always sent.</summary>
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>Supported OAuth grant types, e.g. "authorization_code". Always sent.</summary>
    [JsonPropertyName("grant_types_supported")]
    public List<string> GrantTypesSupported { get; set; } = new List<string>();

    /// <summary>Client authentication methods accepted at the token endpoint.</summary>
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? TokenEndpointAuthMethodsSupported { get; set; }

    /// <summary>Dynamic client registration URL.</summary>
    [JsonPropertyName("registration_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegistrationEndpoint { get; set; }

    /// <summary>Scopes a SMART app can ask for.</summary>
    [JsonPropertyName("scopes_supported")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ScopesSupported { get; set; }

    /// <summary>OAuth response types, e.g. "code".</summary>
    [JsonPropertyName("response_types_supported")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ResponseTypesSupported { get; set; }

    /// <summary>Page where a user can see and revoke app authorizations.</summary>
    [JsonPropertyName("management_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ManagementEndpoint { get; set; }

    /// <summary>Token introspection URL.</summary>
    [JsonPropertyName("introspection_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IntrospectionEndpoint { get; set; }

    /// <summary>Token revocation URL.</summary>
    [JsonPropertyName("revocation_endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RevocationEndpoint { get; set; }

    /// <summary>SMART capabilities, e.g. "launch-ehr", "context-ehr-patient". Always sent.</summary>
    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = new List<string>();

    /// <summary>PKCE methods. Always sent; contains "S256".</summary>
    [JsonPropertyName("code_challenge_methods_supported")]
    public List<string> CodeChallengeMethodsSupported { get; set; } = new List<string>();
}
