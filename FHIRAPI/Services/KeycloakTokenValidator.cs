using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Checks Keycloak access tokens (JWT) locally, without calling Keycloak for every request
/// (Security:TokenProvider = "Keycloak"):
///   signature  Keycloak's public keys (jwks_uri of the realm), read once and cached; read again when Keycloak rotates
///              its keys (a token signed with an unknown key)
///   iss        must be Keycloak:Authority
///   aud        must be one of Keycloak:Audiences (default: the FHIR base URL)
///   exp        not expired (Keycloak:ClockSkewSeconds allowed)
/// Then the claims become a TokenGrant: azp (client), scope (SMART scopes), patient (from the SMART launch add-on).
/// One instance for the whole application (the key cache is shared).
/// </summary>
public class KeycloakTokenValidator : IAccessTokenValidator
{
    private readonly KeycloakSettings _settings;
    private readonly List<string> _audiences;
    private readonly ConfigurationManager<OpenIdConnectConfiguration>? _metadata;
    private readonly JsonWebTokenHandler _handler = new JsonWebTokenHandler();
    private readonly ILogger<KeycloakTokenValidator> _logger;

    /// <summary>Created once by dependency injection.</summary>
    public KeycloakTokenValidator(IOptions<KeycloakSettings> keycloak, IOptions<FhirServerSettings> server,
        ILogger<KeycloakTokenValidator> logger)
    {
        _settings = keycloak.Value;
        _logger = logger;

        _audiences = new List<string>();
        foreach (string audience in _settings.Audiences)
        {
            if (!string.IsNullOrWhiteSpace(audience))
            {
                _audiences.Add(audience.Trim());
            }
        }
        if (_audiences.Count == 0)
        {
            // SMART: the app sends aud = the FHIR base URL; Keycloak's Audience mapper puts the same value in the token
            _audiences.Add(server.Value.BaseUrl);
        }

        if (_settings.IsConfigured())
        {
            HttpDocumentRetriever retriever = new HttpDocumentRetriever();
            retriever.RequireHttps = _settings.RequireHttpsMetadata;
            _metadata = new ConfigurationManager<OpenIdConnectConfiguration>(_settings.GetMetadataAddress(),
                new OpenIdConnectConfigurationRetriever(), retriever);
        }
    }

    /// <inheritdoc />
    public async Task<TokenGrant?> ValidateAsync(string token, CancellationToken ct)
    {
        if (_metadata == null)
        {
            throw new FhirException(StatusCodes.Status503ServiceUnavailable, IssueTypes.Exception,
                "Access tokens cannot be checked: Keycloak:Authority is not set.");
        }

        TokenValidationResult result = await ValidateWithKeysAsync(token, ct);
        if (!result.IsValid && result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // Keycloak may have rotated its keys since they were read: read them again, once
            _metadata.RequestRefresh();
            result = await ValidateWithKeysAsync(token, ct);
        }
        if (!result.IsValid)
        {
            string reason = result.Exception == null ? "unknown reason" : result.Exception.Message;
            _logger.LogInformation("Access token rejected: {Reason}", reason);
            return null;
        }

        JsonWebToken? jwt = result.SecurityToken as JsonWebToken;
        if (jwt == null)
        {
            return null;
        }

        TokenGrant grant = new TokenGrant();
        grant.Subject = ReadClaim(jwt, "azp") ?? ReadClaim(jwt, "client_id") ?? jwt.Subject ?? string.Empty;
        grant.Scope = ReadClaim(jwt, "scope") ?? string.Empty;
        grant.PatientId = ReadClaim(jwt, "patient");
        grant.Audience = _audiences[0];
        grant.ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(jwt.ValidTo, DateTimeKind.Utc));
        return grant;
    }

    // ================================================================== helpers

    /// <summary>Reads Keycloak's keys (cached) and checks the token with them.</summary>
    private async Task<TokenValidationResult> ValidateWithKeysAsync(string token, CancellationToken ct)
    {
        OpenIdConnectConfiguration configuration;
        try
        {
            configuration = await _metadata!.GetConfigurationAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            // the OpenID configuration or the keys could not be read (Keycloak down, wrong address, not https)
            _logger.LogError(ex, "Keycloak keys could not be read from {Address}", _settings.GetMetadataAddress());
            throw new FhirException(StatusCodes.Status503ServiceUnavailable, IssueTypes.Exception,
                "Access tokens cannot be checked right now: Keycloak could not be reached.");
        }

        TokenValidationParameters parameters = new TokenValidationParameters();
        parameters.ValidateIssuer = true;
        parameters.ValidIssuer = _settings.GetIssuer();
        parameters.ValidateAudience = true;
        parameters.ValidAudiences = _audiences;
        parameters.ValidateLifetime = true;
        parameters.RequireExpirationTime = true;
        parameters.RequireSignedTokens = true;
        parameters.ValidateIssuerSigningKey = true;
        parameters.IssuerSigningKeys = configuration.SigningKeys;
        parameters.ClockSkew = TimeSpan.FromSeconds(Math.Max(0, _settings.ClockSkewSeconds));
        return await _handler.ValidateTokenAsync(token, parameters);
    }

    /// <summary>A text claim of the token, or null when it is missing or empty.</summary>
    private static string? ReadClaim(JsonWebToken jwt, string name)
    {
        string? value;
        if (jwt.TryGetPayloadValue<string>(name, out value) && !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
        return null;
    }
}
