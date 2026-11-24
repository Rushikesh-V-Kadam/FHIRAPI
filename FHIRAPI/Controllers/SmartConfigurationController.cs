using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Models.Smart;

namespace FHIRAPI.Controllers;

/// <summary>
/// SMART App Launch discovery: GET /fhir/r4/.well-known/smart-configuration.
/// When the EHR opens a payer's SMART DTR app it passes only "iss" (this FHIR base URL) and "launch". The app's first
/// call is {iss}/.well-known/smart-configuration, which tells it where to log in (authorize URL), where to get the token
/// (token URL) and which SMART features and scopes are supported. Without it a standard SMART app cannot start.
/// This API does not log anyone in itself: the URLs returned point to the client's IdP (Azure Entra ID).
/// All values come from the "SmartConfiguration" appsettings section (Configuration/SmartConfigurationSettings).
/// The answer is the same for every payer: it describes this FHIR server, not a payer.
/// The fixed route here always wins over FhirUnsupportedTypeController's {type}/{id} route.
/// </summary>
[ApiController]
[Route("fhir/r4/.well-known")]
[Produces(FhirMediaTypes.Json)]
[Tags("SMART App Launch - discovery (.well-known/smart-configuration)")]
public class SmartConfigurationController : ControllerBase
{
    private readonly SmartConfigurationSettings _settings;

    /// <summary>Created by dependency injection.</summary>
    public SmartConfigurationController(IOptions<SmartConfigurationSettings> settings)
    {
        _settings = settings.Value;
    }

    /// <summary>Returns the SMART configuration (authorize and token URLs, capabilities, scopes, PKCE methods).</summary>
    /// <remarks>
    /// No parameters and no authentication: the SMART specification requires this document to be public.
    /// Content type is application/json (not application/fhir+json), as the specification requires.
    /// Optional values that are not configured are left out of the response.
    /// Example response:
    /// { "issuer": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    ///   "jwks_uri": "https://login.microsoftonline.com/{tenant-id}/discovery/v2.0/keys",
    ///   "authorization_endpoint": "https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/authorize",
    ///   "token_endpoint": "https://login.microsoftonline.com/{tenant-id}/oauth2/v2.0/token",
    ///   "grant_types_supported": [ "authorization_code" ],
    ///   "token_endpoint_auth_methods_supported": [ "client_secret_post", "client_secret_basic", "private_key_jwt" ],
    ///   "scopes_supported": [ "openid", "fhirUser", "launch", "patient/Patient.rs", "patient/QuestionnaireResponse.cru" ],
    ///   "response_types_supported": [ "code" ],
    ///   "capabilities": [ "launch-ehr", "client-public", "context-ehr-patient", "permission-v2" ],
    ///   "code_challenge_methods_supported": [ "S256" ] }
    /// </remarks>
    /// <response code="200">The SMART configuration document (JSON).</response>
    /// <response code="500">A required setting in the "SmartConfiguration" section is missing. Body: a FHIR OperationOutcome naming the missing settings, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "fatal", "code": "exception", "diagnostics": "SMART configuration is incomplete. Missing settings: SmartConfiguration:TokenEndpoint." } ] }.</response>
    [HttpGet("smart-configuration")]
    [ProducesResponseType(typeof(SmartConfiguration), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError)]
    public IActionResult GetSmartConfiguration()
    {
        CheckRequiredSettings();

        SmartConfiguration configuration = new SmartConfiguration();
        configuration.Issuer = TextOrNull(_settings.Issuer);
        configuration.JwksUri = TextOrNull(_settings.JwksUri);
        configuration.AuthorizationEndpoint = TextOrNull(_settings.AuthorizationEndpoint);
        configuration.TokenEndpoint = _settings.TokenEndpoint.Trim();
        configuration.GrantTypesSupported = _settings.GrantTypesSupported;
        configuration.TokenEndpointAuthMethodsSupported = ListOrNull(_settings.TokenEndpointAuthMethodsSupported);
        configuration.RegistrationEndpoint = TextOrNull(_settings.RegistrationEndpoint);
        configuration.ScopesSupported = ListOrNull(_settings.ScopesSupported);
        configuration.ResponseTypesSupported = ListOrNull(_settings.ResponseTypesSupported);
        configuration.ManagementEndpoint = TextOrNull(_settings.ManagementEndpoint);
        configuration.IntrospectionEndpoint = TextOrNull(_settings.IntrospectionEndpoint);
        configuration.RevocationEndpoint = TextOrNull(_settings.RevocationEndpoint);
        configuration.Capabilities = _settings.Capabilities;
        configuration.CodeChallengeMethodsSupported = _settings.CodeChallengeMethodsSupported;

        return Ok(configuration);
    }

    /// <summary>
    /// Checks the values the SMART specification marks as required, so a wrong setup is reported clearly (500 with the
    /// missing setting names) instead of sending SMART apps an incomplete document.
    /// </summary>
    private void CheckRequiredSettings()
    {
        List<string> missing = new List<string>();

        // Always required by the specification
        if (string.IsNullOrWhiteSpace(_settings.TokenEndpoint))
        {
            missing.Add("SmartConfiguration:TokenEndpoint");
        }
        if (_settings.GrantTypesSupported.Count == 0)
        {
            missing.Add("SmartConfiguration:GrantTypesSupported");
        }
        if (_settings.Capabilities.Count == 0)
        {
            missing.Add("SmartConfiguration:Capabilities");
        }
        if (!_settings.CodeChallengeMethodsSupported.Contains("S256"))
        {
            missing.Add("SmartConfiguration:CodeChallengeMethodsSupported (must contain S256)");
        }

        // Required only for some capabilities
        bool hasLaunch = _settings.Capabilities.Contains("launch-ehr") || _settings.Capabilities.Contains("launch-standalone");
        if (hasLaunch && string.IsNullOrWhiteSpace(_settings.AuthorizationEndpoint))
        {
            missing.Add("SmartConfiguration:AuthorizationEndpoint (needed for launch-ehr / launch-standalone)");
        }
        if (_settings.Capabilities.Contains("sso-openid-connect"))
        {
            if (string.IsNullOrWhiteSpace(_settings.Issuer))
            {
                missing.Add("SmartConfiguration:Issuer (needed for sso-openid-connect)");
            }
            if (string.IsNullOrWhiteSpace(_settings.JwksUri))
            {
                missing.Add("SmartConfiguration:JwksUri (needed for sso-openid-connect)");
            }
        }

        if (missing.Count > 0)
        {
            throw new FhirException(StatusCodes.Status500InternalServerError, IssueTypes.Exception,
                "SMART configuration is incomplete. Missing settings: " + string.Join(", ", missing) + ".");
        }
    }

    /// <summary>The trimmed text, or null when empty (so the JSON leaves the value out).</summary>
    private static string? TextOrNull(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return value.Trim();
    }

    /// <summary>The list, or null when empty (so the JSON leaves the value out).</summary>
    private static List<string>? ListOrNull(List<string> values)
    {
        if (values.Count == 0)
        {
            return null;
        }
        return values;
    }
}
