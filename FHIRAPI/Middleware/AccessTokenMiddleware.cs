using System.Text.Json;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Fhir;
using FHIRAPI.Models.Requests;
using FHIRAPI.Models.Security;
using FHIRAPI.Services;

namespace FHIRAPI.Middleware;

/// <summary>
/// Access checks before the controllers run:
///   /internal/*   X-Api-Key must equal Security:InternalApiKey (when the key is set).
///   /fhir/r4/*    when Security:ValidateAccessTokens is true:
///                 - public: metadata, .well-known/smart-configuration, auth/authorize, auth/token
///                 - X-Api-Key = Security:InternalApiKey: full access (the Payer Gateway)
///                 - otherwise "Authorization: Bearer {token}" issued by this API: not expired, aud = this API, and a
///                   scope that allows the interaction on the resource type (GET by id = r, search = s, POST = c,
///                   PUT = u, DELETE = d). The grant is kept in HttpContext.Items for PatientAccessGuard, which makes
///                   sure a patient token only returns its own patient's data.
/// Failures: 401 (no or bad token) / 403 (scope) with an OperationOutcome; 401 with an OAuth error on /internal.
/// </summary>
public class AccessTokenMiddleware
{
    /// <summary>HttpContext.Items key of the TokenGrant of the current request.</summary>
    public const string GrantItemKey = "SmartTokenGrant";

    /// <summary>Header the Payer Gateway sends its key in.</summary>
    public const string ApiKeyHeader = "X-Api-Key";

    private static readonly PathString FhirRoot = new PathString("/fhir/r4");
    private static readonly PathString InternalRoot = new PathString("/internal");

    /// <summary>Paths under /fhir/r4 that need no token.</summary>
    private static readonly string[] PublicPaths = { "/metadata", "/.well-known/smart-configuration", "/auth/authorize", "/auth/token" };

    private readonly RequestDelegate _next;
    private readonly ILogger<AccessTokenMiddleware> _logger;

    /// <summary>Created once by the pipeline.</summary>
    public AccessTokenMiddleware(RequestDelegate next, ILogger<AccessTokenMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Runs the checks, then the rest of the pipeline.</summary>
    public async Task InvokeAsync(HttpContext context, ITokenService tokens, IOptions<SecuritySettings> security,
        IOptions<FhirServerSettings> server)
    {
        SecuritySettings settings = security.Value;
        PathString path = context.Request.Path;

        // CORS preflight requests carry no credentials; UseCors answers them
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (path.StartsWithSegments(InternalRoot, StringComparison.OrdinalIgnoreCase))
        {
            if (settings.InternalApiKey.Length > 0 && !HasInternalKey(context, settings))
            {
                await WriteOAuthErrorAsync(context, "invalid_client", "Missing or invalid " + ApiKeyHeader + ".");
                return;
            }
            await _next(context);
            return;
        }

        PathString rest;
        if (!settings.ValidateAccessTokens || !path.StartsWithSegments(FhirRoot, StringComparison.OrdinalIgnoreCase, out rest))
        {
            await _next(context);
            return;
        }
        if (IsPublic(rest))
        {
            await _next(context);
            return;
        }
        if (settings.InternalApiKey.Length > 0 && HasInternalKey(context, settings))
        {
            await _next(context);   // the Payer Gateway: no patient or scope limits
            return;
        }

        string? token = ReadBearer(context);
        if (token == null)
        {
            await WriteOutcomeAsync(context, 401, IssueTypes.Security, "An access token is required: Authorization: Bearer {token}.", null);
            return;
        }
        TokenGrant? grant = await tokens.ValidateAsync(token, context.RequestAborted);
        if (grant == null)
        {
            await WriteOutcomeAsync(context, 401, IssueTypes.Security, "The access token is unknown or expired.", "invalid_token");
            return;
        }
        if (!string.Equals(grant.Audience.TrimEnd('/'), server.Value.BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            await WriteOutcomeAsync(context, 401, IssueTypes.Security, "The access token was not issued for this FHIR server.", "invalid_token");
            return;
        }

        string? resourceType;
        char permission;
        if (TryGetInteraction(context.Request.Method, rest, out resourceType, out permission) && resourceType != null)
        {
            if (!ScopeAllows(grant.Scope, resourceType, permission))
            {
                _logger.LogWarning("Token of {Subject} has no scope for {Permission} on {Type}", grant.Subject, permission, resourceType);
                await WriteOutcomeAsync(context, 403, IssueTypes.Forbidden,
                    "The token's scope does not allow this call (" + resourceType + ", " + permission + ").", "insufficient_scope");
                return;
            }
        }

        context.Items[GrantItemKey] = grant;
        await _next(context);
    }

    // ================================================================== helpers

    /// <summary>Binary/{id} is the file of DocumentReference/{id}, so DocumentReference read scopes cover it too.</summary>
    private static bool ScopeAllows(string scope, string resourceType, char permission)
    {
        if (SmartScopeChecker.Allows(scope, resourceType, permission))
        {
            return true;
        }
        if (resourceType == "Binary")
        {
            return SmartScopeChecker.Allows(scope, "DocumentReference", 'r');
        }
        return false;
    }

    /// <summary>True for the paths under /fhir/r4 that need no token.</summary>
    private static bool IsPublic(PathString rest)
    {
        string value = (rest.Value ?? string.Empty).TrimEnd('/');
        foreach (string publicPath in PublicPaths)
        {
            if (string.Equals(value, publicPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// /Type -> search (s) or create (c); /Type/_search -> s; /Type/{id}... -> read (r), update (u) or delete (d).
    /// False when the path names no resource type.
    /// </summary>
    private static bool TryGetInteraction(string method, PathString rest, out string? resourceType, out char permission)
    {
        resourceType = null;
        permission = 'r';
        string[] segments = (rest.Value ?? string.Empty).Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }
        resourceType = segments[0];
        bool hasId = segments.Length > 1 && segments[1] != "_search";

        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
        {
            permission = hasId ? 'r' : 's';
        }
        else if (HttpMethods.IsPost(method))
        {
            permission = segments.Length > 1 && segments[1] == "_search" ? 's' : 'c';
        }
        else if (HttpMethods.IsPut(method) || HttpMethods.IsPatch(method))
        {
            permission = 'u';
        }
        else if (HttpMethods.IsDelete(method))
        {
            permission = 'd';
        }
        return true;
    }

    /// <summary>The token of "Authorization: Bearer {token}", or null.</summary>
    private static string? ReadBearer(HttpContext context)
    {
        string header = context.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (header.Length <= prefix.Length || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        string token = header.Substring(prefix.Length).Trim();
        if (token.Length == 0)
        {
            return null;
        }
        return token;
    }

    /// <summary>True when X-Api-Key equals Security:InternalApiKey (constant-time compare).</summary>
    private static bool HasInternalKey(HttpContext context, SecuritySettings settings)
    {
        string sent = context.Request.Headers[ApiKeyHeader].ToString();
        if (sent.Length == 0)
        {
            return false;
        }
        return RandomIds.SecretEquals(sent, settings.InternalApiKey);
    }

    /// <summary>FHIR error body; 401 also gets the WWW-Authenticate header (RFC 6750).</summary>
    private static async Task WriteOutcomeAsync(HttpContext context, int status, string issueCode, string message, string? oauthError)
    {
        context.Response.StatusCode = status;
        if (status == 401 || status == 403)
        {
            string challenge = "Bearer";
            if (oauthError != null)
            {
                challenge = challenge + " error=\"" + oauthError + "\"";
            }
            context.Response.Headers.WWWAuthenticate = challenge;
        }
        context.Response.ContentType = FhirMediaTypes.FhirJson;
        await context.Response.WriteAsync(FhirJson.Serialize(OperationOutcome.Create("error", issueCode, message)), context.RequestAborted);
    }

    /// <summary>OAuth error body for /internal (401).</summary>
    private static async Task WriteOAuthErrorAsync(HttpContext context, string error, string description)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = FhirMediaTypes.Json;
        string body = JsonSerializer.Serialize(new OAuthError(error, description));
        await context.Response.WriteAsync(body, context.RequestAborted);
    }
}
