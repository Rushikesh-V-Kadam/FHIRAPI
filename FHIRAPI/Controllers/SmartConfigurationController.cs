using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Models.Requests;

namespace FHIRAPI.Controllers;

/// <summary>
/// SMART App Launch discovery for a payer's DTR app:
///   GET {fhir}/.well-known/smart-configuration   where to sign in and get the token
/// {fhir} is /fhir/r4 (the "iss" the app was launched with). Security:TokenProvider decides what it points to:
///   BuiltIn   this API's auth/authorize and auth/token (SmartAuthController)
///   Keycloak  Keycloak's endpoints; the Keycloak SMART launch add-on adds the launch context to the token response
/// The tokens are checked on every /fhir/r4 call by AccessTokenMiddleware (Security:ValidateAccessTokens).
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Tags("SMART launch context - called by payer DTR apps")]
public class SmartConfigurationController : ControllerBase
{
    private readonly KeycloakSettings _keycloak;
    private readonly SecuritySettings _security;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public SmartConfigurationController(IOptions<KeycloakSettings> keycloak, IOptions<SecuritySettings> security,
        IOptions<FhirServerSettings> server)
    {
        _keycloak = keycloak.Value;
        _security = security.Value;
        _server = server.Value;
    }

    /// <summary>SMART discovery document. The app calls this first, with the "iss" it received.</summary>
    /// <response code="200">The SMART configuration (this API's endpoints, or Keycloak's).</response>
    /// <response code="503">Security:TokenProvider is Keycloak but Keycloak:Authority is not set.</response>
    [HttpGet(".well-known/smart-configuration")]
    [Produces(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(SmartConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<SmartConfigurationResponse> GetConfiguration()
    {
        if (!_security.UsesKeycloak())
        {
            return BuiltInConfiguration();
        }
        if (!_keycloak.IsConfigured())
        {
            throw new FhirException(StatusCodes.Status503ServiceUnavailable, IssueTypes.Exception,
                "SMART App Launch is not available: Security:TokenProvider is Keycloak but Keycloak:Authority is not set.");
        }

        SmartConfigurationResponse configuration = new SmartConfigurationResponse();
        configuration.Issuer = _keycloak.GetIssuer();
        configuration.JwksUri = _keycloak.GetJwksUri();
        configuration.AuthorizationEndpoint = _keycloak.GetAuthorizationEndpoint();
        configuration.TokenEndpoint = _keycloak.GetTokenEndpoint();
        configuration.RevocationEndpoint = _keycloak.GetRevocationEndpoint();
        configuration.IntrospectionEndpoint = _keycloak.GetIntrospectionEndpoint();
        return configuration;
    }

    /// <summary>
    /// BuiltIn: this API's endpoints. Only public apps (PKCE, no client secret), no id_token (openid) and no refresh token.
    /// </summary>
    private SmartConfigurationResponse BuiltInConfiguration()
    {
        string baseUrl = _server.BaseUrl;
        SmartConfigurationResponse configuration = new SmartConfigurationResponse();
        configuration.Issuer = baseUrl;
        configuration.AuthorizationEndpoint = baseUrl + "/auth/authorize";
        configuration.TokenEndpoint = baseUrl + "/auth/token";
        configuration.GrantTypesSupported = new string[] { "authorization_code" };
        configuration.TokenEndpointAuthMethodsSupported = new string[] { "none" };
        configuration.ScopesSupported = new string[] { "launch", "fhirUser", "patient/*.rs", "user/*.rs", "patient/QuestionnaireResponse.cu", "patient/*.read" };
        configuration.Capabilities = new string[] { "launch-ehr", "authorize-post", "client-public", "context-ehr-patient",
            "context-ehr-encounter", "permission-v1", "permission-v2", "permission-patient", "permission-user" };
        return configuration;
    }
}
