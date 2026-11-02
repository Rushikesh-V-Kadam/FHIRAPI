using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Models.Requests;
using FHIRAPI.Models.Security;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// Endpoints for the internal network only. When Security:InternalApiKey is set, every call must send it in the
/// X-Api-Key header (checked by AccessTokenMiddleware).
///   POST /internal/access-tokens                      BuiltIn only: short-lived, one-patient token the gateway gives the
///                                                     payer as CDS Hooks "fhirAuthorization" (404 with Keycloak)
///   POST /internal/smart-launches                     Payer Gateway: SMART launch context when a user opens a DTR link,
///                                                     or (Keycloak) before it asks Keycloak for a CRD fhirAuthorization token
///   GET  /internal/smart-launches/{launchId}/context  Keycloak only: the context the Keycloak SMART launch add-on puts
///                                                     next to the token (404 with BuiltIn)
/// </summary>
[ApiController]
[Route("internal")]
[Produces(FhirMediaTypes.Json)]
[Tags("Internal - called by the Payer Gateway and Keycloak only")]
public class InternalController : ControllerBase
{
    /// <summary>Longest lifetime of a BuiltIn fhirAuthorization token, in minutes.</summary>
    private const int MaxTokenMinutes = 15;

    private readonly ITokenService _tokens;
    private readonly ISmartLaunchStore _launches;
    private readonly FhirServerSettings _server;
    private readonly SecuritySettings _security;
    private readonly ILogger<InternalController> _logger;

    /// <summary>Created by dependency injection.</summary>
    public InternalController(ITokenService tokens, ISmartLaunchStore launches, IOptions<FhirServerSettings> server,
        IOptions<SecuritySettings> security, ILogger<InternalController> logger)
    {
        _tokens = tokens;
        _launches = launches;
        _server = server.Value;
        _security = security.Value;
        _logger = logger;
    }

    /// <summary>
    /// BuiltIn only: issues a short-lived access token for one patient, in the CDS Hooks "fhirAuthorization" shape.
    /// The gateway sends it to the payer inside the CRD hook call, so the payer can read extra data that was not in the
    /// prefetch. With Security:ValidateAccessTokens the token is checked on every /fhir/r4 call: only this patient,
    /// only these scopes, until it expires. With Keycloak the gateway gets this token from Keycloak instead.
    /// </summary>
    /// <param name="request">Required. subject (payer) and patientId must be filled; scope and minutes are optional.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The token, ready to send as fhirAuthorization.</response>
    /// <response code="400">subject or patientId is missing.</response>
    /// <response code="404">Keycloak issues the tokens (Security:TokenProvider = Keycloak).</response>
    [HttpPost("access-tokens")]
    [Consumes(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(AccessTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AccessTokenResponse>> IssueAccessToken([FromBody, Required] AccessTokenRequest request, CancellationToken ct)
    {
        if (_security.UsesKeycloak())
        {
            return NotFound(new OAuthError("invalid_request", "Tokens are issued by Keycloak (Security:TokenProvider = Keycloak)."));
        }
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
        grant.Audience = _server.BaseUrl;
        grant.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(minutes);
        string token = await _tokens.IssueAsync(grant, ct);
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
    /// Creates a SMART launch context (patient, user, encounter, orders/coverage, appContext).
    /// DTR: the gateway then redirects the browser to {DTR app}?iss={iss}&amp;launch={launchId}; the app sends launch to
    /// Keycloak, and Keycloak reads the context from GET /internal/smart-launches/{launchId}/context.
    /// CRD: the gateway sends launch with its client_credentials token request, so the payer's fhirAuthorization token
    /// is limited to the patient. "iss" is the FHIR R4 base URL (FhirServer:PublicBaseUrl).
    /// </summary>
    /// <param name="request">Required. patientId must be filled; insurancePlanId, user, encounter, fhirContext and appContext are optional.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The launch id and iss.</response>
    /// <response code="400">patientId is missing.</response>
    [HttpPost("smart-launches")]
    [Consumes(FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(SmartLaunchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SmartLaunchResponse>> CreateSmartLaunch([FromBody, Required] SmartLaunchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId))
        {
            return BadRequest(new OAuthError("invalid_request", "patientId is required."));
        }

        SmartLaunch launch = new SmartLaunch();
        launch.PatientId = request.PatientId;
        launch.InsurancePlanId = request.InsurancePlanId;
        launch.UserPractitionerId = request.UserPractitionerId;
        launch.EncounterId = request.EncounterId;
        launch.FhirContext = request.FhirContext;
        launch.AppContext = request.AppContext;
        await _launches.SaveLaunchAsync(launch, ct);
        _logger.LogInformation("SMART launch {LaunchId} created for patient {PatientId}", launch.LaunchId, request.PatientId);

        SmartLaunchResponse response = new SmartLaunchResponse();
        response.LaunchId = launch.LaunchId;
        response.Iss = _server.BaseUrl;
        response.ExpiresAt = launch.ExpiresAt;
        return response;
    }

    /// <summary>
    /// The launch context of a launch id, for the Keycloak SMART launch add-on (called while Keycloak issues the token).
    /// A launch made for one payer (insurancePlanId) may only be used by a Keycloak client of that payer: the add-on
    /// sends the client's "insurancePlanId" attribute.
    /// </summary>
    /// <param name="launchId">The launch id the app (or the gateway) sent to Keycloak.</param>
    /// <param name="clientId">Optional. Keycloak client asking for the token (written to the log).</param>
    /// <param name="insurancePlanId">Optional. insurancePlanId attribute of that Keycloak client. Required when the launch is for one payer.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The launch context.</response>
    /// <response code="403">The launch is for another payer's app.</response>
    /// <response code="404">Unknown or expired launch, or BuiltIn issues the tokens (no Keycloak).</response>
    [HttpGet("smart-launches/{launchId}/context")]
    [ProducesResponseType(typeof(SmartLaunchContextResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(OAuthError), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SmartLaunchContextResponse>> GetLaunchContext(string launchId, [FromQuery] string? clientId,
        [FromQuery] int? insurancePlanId, CancellationToken ct)
    {
        if (!_security.UsesKeycloak())
        {
            return NotFound(new OAuthError("invalid_request", "Only used with Keycloak (Security:TokenProvider = Keycloak)."));
        }
        SmartLaunch? launch = await _launches.GetLaunchAsync(launchId, ct);
        if (launch == null)
        {
            return NotFound(new OAuthError("invalid_request", "Unknown or expired launch. Open the documentation link again."));
        }
        if (launch.InsurancePlanId.HasValue && launch.InsurancePlanId != insurancePlanId)
        {
            _logger.LogWarning("Keycloak client {ClientId} (insurancePlanId {PlanId}) tried to use launch {LaunchId} of insurancePlanId {LaunchPlanId}",
                clientId, insurancePlanId, launchId, launch.InsurancePlanId);
            return StatusCode(StatusCodes.Status403Forbidden, new OAuthError("access_denied", "This launch is for another payer's app."));
        }

        SmartLaunchContextResponse response = new SmartLaunchContextResponse();
        response.Patient = launch.PatientId;
        response.Encounter = launch.EncounterId;
        if (!string.IsNullOrWhiteSpace(launch.UserPractitionerId))
        {
            response.FhirUser = "Practitioner/" + launch.UserPractitionerId;
        }
        foreach (string reference in launch.FhirContext)
        {
            FhirContextItem item = new FhirContextItem();
            item.Reference = reference;
            response.FhirContext.Add(item);
        }
        response.AppContext = launch.AppContext;
        response.NeedPatientBanner = false;
        _logger.LogInformation("Launch context of {LaunchId} given to Keycloak client {ClientId}", launchId, clientId);
        return response;
    }
}
