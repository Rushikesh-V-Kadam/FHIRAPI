using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Models.Requests;
using FHIRAPI.Models.Security;
using FHIRAPI.Services;
using FHIRAPI.Versioning;

namespace FHIRAPI.Controllers;

/// <summary>
/// Endpoints for the Payer Gateway only (keep them on the internal network):
///   POST /internal/access-tokens   short-lived, one-patient token the gateway gives the payer as CDS Hooks "fhirAuthorization"
///   POST /internal/smart-launches  SMART launch context for a payer DTR app when a user opens a DTR link
/// </summary>
[ApiController]
[Route("internal")]
[Produces(FhirMediaTypes.Json)]
[Tags("4. Internal - called by the Payer Gateway only")]
public class InternalController : ControllerBase
{
    /// <summary>Longest lifetime of a fhirAuthorization token, in minutes.</summary>
    private const int MaxTokenMinutes = 15;

    private readonly ITokenService _tokens;
    private readonly ISmartLaunchStore _launches;
    private readonly FhirServerSettings _server;
    private readonly ILogger<InternalController> _logger;

    /// <summary>Created by dependency injection.</summary>
    public InternalController(ITokenService tokens, ISmartLaunchStore launches, IOptions<FhirServerSettings> server,
        ILogger<InternalController> logger)
    {
        _tokens = tokens;
        _launches = launches;
        _server = server.Value;
        _logger = logger;
    }

    /// <summary>
    /// Issues a short-lived access token for one patient, in the CDS Hooks "fhirAuthorization" shape.
    /// The gateway sends it to the payer inside the CRD hook call, so the payer can read extra data that was not in the
    /// prefetch. The token is issued and kept (ITokenService), but no endpoint of this API checks it: authentication
    /// is added outside this code.
    /// </summary>
    /// <param name="request">Required. subject (payer) and patientId must be filled; scope and minutes are optional.</param>
    /// <response code="200">The token, ready to send as fhirAuthorization.</response>
    /// <response code="400">subject or patientId is missing.</response>
    [HttpPost("access-tokens")]
    [Consumes(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    public ActionResult<AccessTokenResponse> IssueAccessToken([FromBody, Required] AccessTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.PatientId))
        {
            return BadRequest(new OAuthError("invalid_request", "subject and patientId are required."));
        }

        int minutes = Math.Clamp(request.Minutes, 1, MaxTokenMinutes);
        string scope = string.IsNullOrWhiteSpace(request.Scope) ? SmartScopes.PayerDefault : request.Scope;

        TokenGrant grant = new TokenGrant();
        grant.Subject = request.Subject;
        grant.Scope = scope;
        grant.PatientId = request.PatientId;
        grant.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes);
        string token = _tokens.Issue(grant);
        _logger.LogInformation("Token issued to {Subject} for patient {PatientId} ({Minutes} min)", request.Subject, request.PatientId, minutes);

        AccessTokenResponse response = new AccessTokenResponse();
        response.AccessToken = token;
        response.ExpiresIn = minutes * 60;
        response.Scope = scope;
        response.Subject = request.Subject;
        response.Patient = request.PatientId;
        return response;
    }

    /// <summary>
    /// Creates a SMART launch context for a DTR app (patient, user, encounter, orders/coverage, appContext).
    /// The gateway then redirects the browser to {DTR app}?iss={iss}&amp;launch={launchId}.
    /// "iss" is the R4 base URL, or the R5 base URL when the request says fhirVersion = "r5".
    /// </summary>
    /// <param name="request">Required. patientId must be filled; user, encounter, fhirContext, appContext and fhirVersion are optional.</param>
    /// <response code="200">The launch id and iss.</response>
    /// <response code="400">patientId is missing, or fhirVersion is not "r4" or "r5".</response>
    [HttpPost("smart-launches")]
    [Consumes(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(SmartLaunchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    public ActionResult<SmartLaunchResponse> CreateSmartLaunch([FromBody, Required] SmartLaunchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId))
        {
            return BadRequest(new OAuthError("invalid_request", "patientId is required."));
        }

        FhirRelease? release = FhirRelease.FromName(request.FhirVersion);
        if (release == null)
        {
            return BadRequest(new OAuthError("invalid_request", "fhirVersion must be \"r4\" or \"r5\"."));
        }

        SmartLaunch launch = new SmartLaunch();
        launch.PatientId = request.PatientId;
        launch.UserPractitionerId = request.UserPractitionerId;
        launch.EncounterId = request.EncounterId;
        launch.FhirContext = request.FhirContext;
        launch.AppContext = request.AppContext;
        launch.FhirVersion = release.Name;
        _launches.SaveLaunch(launch);
        _logger.LogInformation("SMART launch {LaunchId} created for patient {PatientId}, {Release}", launch.LaunchId, request.PatientId, release.Name);

        SmartLaunchResponse response = new SmartLaunchResponse();
        response.LaunchId = launch.LaunchId;
        response.Iss = release.BaseUrl(_server);
        response.ExpiresAt = launch.ExpiresAt;
        return response;
    }
}
