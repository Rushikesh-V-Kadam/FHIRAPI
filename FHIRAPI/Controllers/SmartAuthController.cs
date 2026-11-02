using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Requests;
using FHIRAPI.Models.Security;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// SMART App Launch "EHR launch" (SMART App Launch 2.x) for a payer's DTR app, when this API issues the tokens itself
/// (Security:TokenProvider = "BuiltIn"). With "Keycloak" both endpoints answer 404: sign-in and tokens are at Keycloak.
/// The discovery document (.well-known/smart-configuration) is in SmartConfigurationController.
///   GET/POST  {fhir}/auth/authorize                    one-time code sent to the app's redirect_uri
///   POST      {fhir}/auth/token                        code -> access token + launch context (patient, encounter, user, orders, appContext)
/// {fhir} is /fhir/r4 (the "iss" the app was launched with).
///
/// Checks: registered app (client_id) and exact redirect_uri, response_type=code, state, aud = this API, PKCE S256,
/// "launch" scope and a valid launch that belongs to the app's payer; at token: one-time code, same client_id and
/// redirect_uri, code_verifier. Only public apps (no client secret). No id_token (openid) and no refresh token.
/// The access token is checked on every /fhir/r4 call by AccessTokenMiddleware (Security:ValidateAccessTokens).
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Tags("SMART launch context - called by payer DTR apps")]
public class SmartAuthController : ControllerBase
{
    private readonly ISmartLaunchStore _launches;
    private readonly ISmartClientStore _clients;
    private readonly ITokenService _tokens;
    private readonly FhirServerSettings _server;
    private readonly SecuritySettings _security;
    private readonly ILogger<SmartAuthController> _logger;

    /// <summary>Created by dependency injection.</summary>
    public SmartAuthController(ISmartLaunchStore launches, ISmartClientStore clients, ITokenService tokens,
        IOptions<FhirServerSettings> server, IOptions<SecuritySettings> security, ILogger<SmartAuthController> logger)
    {
        _launches = launches;
        _clients = clients;
        _tokens = tokens;
        _server = server.Value;
        _security = security.Value;
        _logger = logger;
    }

    /// <summary>Authorize (query string). Redirects (302) to redirect_uri?code=...&amp;state=...</summary>
    /// <param name="request">response_type, client_id, redirect_uri, launch, scope, state, aud, code_challenge, code_challenge_method.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="302">Redirect to the app with a code, or with error / error_description / state.</response>
    /// <response code="400">client_id unknown or disabled, or redirect_uri missing or not registered (not redirected: the address is not trusted).</response>
    /// <response code="404">Keycloak issues the tokens (Security:TokenProvider = Keycloak).</response>
    [HttpGet("auth/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Authorize([FromQuery] AuthorizeRequest request, CancellationToken ct)
    {
        if (_security.UsesKeycloak())
        {
            return NotFound(KeycloakIssuesTokens());
        }
        return await AuthorizeCoreAsync(request, ct);
    }

    /// <summary>Authorize sent as a form post (SMART "authorize-post"). Same answers as the GET.</summary>
    /// <param name="request">The same fields as the GET, as application/x-www-form-urlencoded.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="302">Redirect to the app with a code, or with an error.</response>
    /// <response code="400">client_id unknown or disabled, or redirect_uri missing or not registered.</response>
    /// <response code="404">Keycloak issues the tokens (Security:TokenProvider = Keycloak).</response>
    [HttpPost("auth/authorize")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AuthorizePost([FromForm] AuthorizeRequest request, CancellationToken ct)
    {
        if (_security.UsesKeycloak())
        {
            return NotFound(KeycloakIssuesTokens());
        }
        return await AuthorizeCoreAsync(request, ct);
    }

    /// <summary>
    /// Exchanges the one-time code for an access token and the launch context: patient, encounter, fhirUser,
    /// fhirContext (orders, coverage) and appContext (from the payer's CRD card).
    /// </summary>
    /// <param name="request">grant_type, code, redirect_uri, client_id, code_verifier.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">Access token and launch context.</response>
    /// <response code="400">Missing field, wrong grant type, unknown / used / expired code, or a value that does not match authorize.</response>
    /// <response code="404">Keycloak issues the tokens (Security:TokenProvider = Keycloak).</response>
    [HttpPost("auth/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(SmartTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Token([FromForm] TokenRequest request, CancellationToken ct)
    {
        // SMART / OAuth: token responses must never be cached
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";

        if (_security.UsesKeycloak())
        {
            return NotFound(KeycloakIssuesTokens());
        }

        if (request.GrantType != "authorization_code")
        {
            return BadRequest(new OAuthError("unsupported_grant_type", "Only authorization_code is supported."));
        }
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.RedirectUri)
            || string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.CodeVerifier))
        {
            return BadRequest(new OAuthError("invalid_request", "code, redirect_uri, client_id and code_verifier are required."));
        }

        // Codes work once only: TakeCodeAsync removes it, also when a check below fails
        AuthorizationCode? authorization = await _launches.TakeCodeAsync(request.Code, ct);
        if (authorization == null)
        {
            return BadRequest(new OAuthError("invalid_grant", "Unknown, used or expired code."));
        }
        if (authorization.ClientId != request.ClientId)
        {
            return BadRequest(new OAuthError("invalid_grant", "The code was issued to another client_id."));
        }
        if (authorization.RedirectUri != request.RedirectUri)
        {
            return BadRequest(new OAuthError("invalid_grant", "redirect_uri is not the one sent to authorize."));
        }
        if (!PkceMatches(request.CodeVerifier, authorization.CodeChallenge))
        {
            return BadRequest(new OAuthError("invalid_grant", "code_verifier does not match code_challenge."));
        }

        SmartLaunch? launch = await _launches.GetLaunchAsync(authorization.LaunchId, ct);
        if (launch == null)
        {
            return BadRequest(new OAuthError("invalid_grant", "The launch has expired. Open the documentation link again."));
        }

        int seconds = Math.Max(60, _security.SmartTokenSeconds);
        TokenGrant grant = new TokenGrant();
        grant.Subject = authorization.ClientId;
        grant.Scope = authorization.Scope;
        grant.PatientId = launch.PatientId;
        grant.Audience = _server.BaseUrl;
        grant.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(seconds);
        string token = await _tokens.IssueAsync(grant, ct);
        _logger.LogInformation("SMART token issued to {ClientId} for launch {LaunchId}", authorization.ClientId, launch.LaunchId);

        SmartTokenResponse response = new SmartTokenResponse();
        response.AccessToken = token;
        response.ExpiresIn = seconds;
        response.Scope = authorization.Scope;
        response.Patient = launch.PatientId;
        response.Encounter = launch.EncounterId;
        response.FhirUser = launch.UserPractitionerId == null ? null : "Practitioner/" + launch.UserPractitionerId;
        foreach (string reference in launch.FhirContext)
        {
            FhirContextItem item = new FhirContextItem();
            item.Reference = reference;
            response.FhirContext.Add(item);
        }
        response.AppContext = launch.AppContext;
        response.NeedPatientBanner = false;
        return Ok(response);
    }

    // ================================================================== helpers

    /// <summary>The answer of auth/* when Keycloak issues the tokens.</summary>
    private static OAuthError KeycloakIssuesTokens()
    {
        return new OAuthError("invalid_request", "Sign-in and tokens are at Keycloak (Security:TokenProvider = Keycloak); see .well-known/smart-configuration.");
    }

    /// <summary>Shared by GET and POST authorize: checks the request, then launch id -> one-time code.</summary>
    private async Task<IActionResult> AuthorizeCoreAsync(AuthorizeRequest request, CancellationToken ct)
    {
        // 1. The app and its redirect_uri. Until both are verified, errors are answered here (400), never redirected:
        //    sending a browser to an unregistered address would make this API an open redirector.
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return BadRequest(new OAuthError("invalid_request", "client_id is required."));
        }
        SmartClient? client = await _clients.GetClientAsync(request.ClientId, ct);
        if (client == null || !client.Enabled)
        {
            return BadRequest(new OAuthError("unauthorized_client", "client_id is not registered or is disabled."));
        }
        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return BadRequest(new OAuthError("invalid_request", "redirect_uri is required."));
        }
        string redirectUri = request.RedirectUri;
        if (!client.RedirectUris.Contains(redirectUri))
        {
            return BadRequest(new OAuthError("invalid_request", "redirect_uri is not registered for this client_id."));
        }

        // 2. From here on, errors go back to the app's redirect_uri (with state, when the app sent one)
        string state = request.State ?? string.Empty;
        if (string.IsNullOrWhiteSpace(request.State))
        {
            return RedirectError(redirectUri, "invalid_request", "state is required.", state);
        }
        if (request.ResponseType != "code")
        {
            return RedirectError(redirectUri, "unsupported_response_type", "response_type must be code.", state);
        }
        if (string.IsNullOrWhiteSpace(request.Aud) || !SameUrl(request.Aud, _server.BaseUrl))
        {
            return RedirectError(redirectUri, "invalid_request", "aud must be " + _server.BaseUrl + ".", state);
        }
        if (string.IsNullOrWhiteSpace(request.CodeChallenge))
        {
            return RedirectError(redirectUri, "invalid_request", "code_challenge is required (PKCE).", state);
        }
        if (request.CodeChallengeMethod != "S256")
        {
            return RedirectError(redirectUri, "invalid_request", "code_challenge_method must be S256.", state);
        }
        if (!SmartScopeChecker.Contains(request.Scope, "launch"))
        {
            return RedirectError(redirectUri, "invalid_scope", "scope must contain launch (EHR launch).", state);
        }

        // 3. The launch: it must exist and belong to this app's payer
        if (string.IsNullOrWhiteSpace(request.Launch))
        {
            return RedirectError(redirectUri, "invalid_request", "launch is required.", state);
        }
        SmartLaunch? launch = await _launches.GetLaunchAsync(request.Launch, ct);
        if (launch == null)
        {
            return RedirectError(redirectUri, "invalid_request", "unknown or expired launch", state);
        }
        if (launch.InsurancePlanId.HasValue && client.InsurancePlanId.HasValue
            && launch.InsurancePlanId.Value != client.InsurancePlanId.Value)
        {
            _logger.LogWarning("SMART client {ClientId} tried to use launch {LaunchId} of another payer", client.ClientId, launch.LaunchId);
            return RedirectError(redirectUri, "access_denied", "This launch is for another payer's app.", state);
        }

        string grantedScope = SmartScopeChecker.Grant(request.Scope, client.AllowedScopes);

        AuthorizationCode code = new AuthorizationCode();
        code.ClientId = client.ClientId;
        code.RedirectUri = redirectUri;
        code.Scope = grantedScope;
        code.CodeChallenge = request.CodeChallenge;
        code.LaunchId = launch.LaunchId;
        await _launches.SaveCodeAsync(code, ct);

        return Redirect(AddQuery(redirectUri, "code=" + Uri.EscapeDataString(code.Code) + "&state=" + Uri.EscapeDataString(state)));
    }

    /// <summary>302 to the app with an OAuth error.</summary>
    private RedirectResult RedirectError(string redirectUri, string error, string description, string state)
    {
        string query = "error=" + Uri.EscapeDataString(error) + "&error_description=" + Uri.EscapeDataString(description);
        if (state.Length > 0)
        {
            query = query + "&state=" + Uri.EscapeDataString(state);
        }
        return Redirect(AddQuery(redirectUri, query));
    }

    /// <summary>PKCE S256: base64url(SHA-256(code_verifier)) must equal the code_challenge sent to authorize.</summary>
    private static bool PkceMatches(string verifier, string challenge)
    {
        // RFC 7636: 43 to 128 characters
        if (verifier.Length < 43 || verifier.Length > 128 || string.IsNullOrEmpty(challenge))
        {
            return false;
        }
        return RandomIds.SecretEquals(RandomIds.Sha256Base64Url(verifier), challenge);
    }

    /// <summary>Two URLs are the same apart from a trailing slash and letter case.</summary>
    private static bool SameUrl(string a, string b)
    {
        return string.Equals(a.TrimEnd('/'), b.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Appends a query string to a URL that may already have one.</summary>
    private static string AddQuery(string url, string query)
    {
        return url + (url.Contains('?') ? "&" : "?") + query;
    }
}
