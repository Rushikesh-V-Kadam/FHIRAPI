using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Requests;
using FHIRAPI.Models.Security;
using FHIRAPI.Services;
using FHIRAPI.Versioning;

namespace FHIRAPI.Controllers;

/// <summary>
/// SMART App Launch "EHR launch" steps a DTR app (ours, or a payer's own) goes through to learn its launch context:
///   GET       {fhir}/.well-known/smart-configuration   where to authorize and get the context
///   GET/POST  {fhir}/auth/authorize                    one-time code sent to the app's redirect_uri
///   POST      {fhir}/auth/token                        code -> launch context (patient, encounter, user, orders, appContext)
/// {fhir} is /fhir/r4 or /fhir/r5 (the "iss" the app was launched with).
///
/// NO AUTHENTICATION: apps, redirect URIs, PKCE and scopes are not checked, and the access_token returned is a random
/// value no endpoint checks. These endpoints exist only because a SMART app gets its patient / orders / questionnaire
/// context this way. Authentication is added outside this code.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Route("fhir/r5")]
[Tags("3. SMART launch context - called by payer DTR apps")]
public class SmartAuthController : ControllerBase
{
    /// <summary>Lifetime reported to the app (expires_in), in seconds.</summary>
    private const int TokenSeconds = 3600;

    private readonly ISmartLaunchStore _launches;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public SmartAuthController(ISmartLaunchStore launches, IOptions<FhirServerSettings> server)
    {
        _launches = launches;
        _server = server.Value;
    }

    /// <summary>SMART discovery document. The app calls this first, with the "iss" it received.</summary>
    /// <response code="200">The SMART configuration.</response>
    [HttpGet(".well-known/smart-configuration")]
    [Produces(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(SmartConfigurationResponse), StatusCodes.Status200OK)]
    public ActionResult<SmartConfigurationResponse> GetConfiguration()
    {
        string baseUrl = FhirRelease.FromRequest(Request).BaseUrl(_server);
        SmartConfigurationResponse configuration = new SmartConfigurationResponse();
        configuration.Issuer = baseUrl;
        configuration.AuthorizationEndpoint = baseUrl + "/auth/authorize";
        configuration.TokenEndpoint = baseUrl + "/auth/token";
        return configuration;
    }

    /// <summary>Authorize (query string). Redirects (302) to redirect_uri?code=...&amp;state=...</summary>
    /// <param name="request">response_type, client_id, redirect_uri, launch, scope, state (others are ignored).</param>
    /// <response code="302">Redirect to the app with a code (or error=invalid_request for an unknown launch).</response>
    /// <response code="400">redirect_uri missing.</response>
    [HttpGet("auth/authorize")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    public IActionResult Authorize([FromQuery] AuthorizeRequest request)
    {
        return AuthorizeCore(request);
    }

    /// <summary>Authorize sent as a form post (SMART "authorize-post"). Same answers as the GET.</summary>
    /// <param name="request">The same fields as the GET, as application/x-www-form-urlencoded.</param>
    /// <response code="302">Redirect to the app with a code (or an error).</response>
    /// <response code="400">redirect_uri missing.</response>
    [HttpPost("auth/authorize")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    public IActionResult AuthorizePost([FromForm] AuthorizeRequest request)
    {
        return AuthorizeCore(request);
    }

    /// <summary>
    /// Exchanges the one-time code for the launch context: patient, encounter, fhirUser, fhirContext (orders, coverage)
    /// and appContext (from the payer's CRD card). The access_token is a random value; no endpoint checks it.
    /// </summary>
    /// <param name="request">grant_type, code, redirect_uri.</param>
    /// <response code="200">Launch context.</response>
    /// <response code="400">Wrong grant type, or unknown / used / expired code.</response>
    [HttpPost("auth/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(SmartTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    public IActionResult Token([FromForm] TokenRequest request)
    {
        Response.Headers.CacheControl = "no-store";
        if (request.GrantType != "authorization_code")
        {
            return BadRequest(new OAuthError("unsupported_grant_type", "Only authorization_code is supported."));
        }

        // Codes work once only: TakeCode removes it
        AuthorizationCode? authorization = request.Code == null ? null : _launches.TakeCode(request.Code);
        if (authorization == null)
        {
            return BadRequest(new OAuthError("invalid_grant", "Unknown, used or expired code."));
        }

        SmartLaunch launch = authorization.Launch;
        SmartTokenResponse response = new SmartTokenResponse();
        response.AccessToken = RandomIds.New(32);
        response.ExpiresIn = TokenSeconds;
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

    /// <summary>Shared by GET and POST authorize: launch id -> one-time code.</summary>
    private IActionResult AuthorizeCore(AuthorizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            return BadRequest(new OAuthError("invalid_request", "redirect_uri is required."));
        }
        string redirectUri = request.RedirectUri;
        string state = request.State ?? string.Empty;

        SmartLaunch? launch = request.Launch == null ? null : _launches.GetLaunch(request.Launch);
        if (launch == null)
        {
            return Redirect(AddQuery(redirectUri, "error=invalid_request&error_description=" +
                Uri.EscapeDataString("unknown or expired launch") + "&state=" + Uri.EscapeDataString(state)));
        }

        AuthorizationCode code = new AuthorizationCode();
        code.ClientId = request.ClientId ?? string.Empty;
        code.RedirectUri = redirectUri;
        code.Scope = string.IsNullOrWhiteSpace(request.Scope) ? SmartScopes.DtrDefault : request.Scope;
        code.Launch = launch;
        _launches.SaveCode(code);

        return Redirect(AddQuery(redirectUri, "code=" + Uri.EscapeDataString(code.Code) + "&state=" + Uri.EscapeDataString(state)));
    }

    /// <summary>Appends a query string to a URL that may already have one.</summary>
    private static string AddQuery(string url, string query)
    {
        return url + (url.Contains('?') ? "&" : "?") + query;
    }
}
