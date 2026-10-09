using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Body of GET /fhir/r4/.well-known/smart-configuration (SMART App Launch discovery document).
/// </summary>
public class SmartConfigurationResponse
{
    /// <summary>FHIR base URL of this API (the "iss" the app was launched with).</summary>
    /// <example>http://localhost:5100/fhir/r4</example>
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Where the app asks for a code.</summary>
    /// <example>http://localhost:5100/fhir/r4/auth/authorize</example>
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    /// <summary>Where the app exchanges the code for the launch context.</summary>
    /// <example>http://localhost:5100/fhir/r4/auth/token</example>
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;


    /// <summary>Only "authorization_code".</summary>
    [JsonPropertyName("grant_types_supported")]
    public string[] GrantTypesSupported { get; set; } = { "authorization_code" };

    /// <summary>"none": no client authentication.</summary>
    [JsonPropertyName("token_endpoint_auth_methods_supported")]
    public string[] TokenEndpointAuthMethodsSupported { get; set; } = { "none" };


    /// <summary>PKCE method named for SMART clients (PKCE values are not checked).</summary>
    [JsonPropertyName("code_challenge_methods_supported")]
    public string[] CodeChallengeMethodsSupported { get; set; } = { "S256" };

    /// <summary>Only "code".</summary>
    [JsonPropertyName("response_types_supported")]
    public string[] ResponseTypesSupported { get; set; } = { "code" };

    /// <summary>Scopes an app may ask for (scopes are not checked).</summary>
    [JsonPropertyName("scopes_supported")]
    public string[] ScopesSupported { get; set; } =
        { "launch", "fhirUser", "patient/*.rs", "user/*.rs", "patient/QuestionnaireResponse.cu", "patient/*.read" };

    /// <summary>SMART capabilities of this server.</summary>
    [JsonPropertyName("capabilities")]
    public string[] Capabilities { get; set; } =
    { "launch-ehr", "authorize-post", "client-public", "context-ehr-patient", "context-ehr-encounter" };
}
