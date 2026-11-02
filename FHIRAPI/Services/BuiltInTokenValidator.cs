using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Checks the tokens this API issued itself (Security:TokenProvider = "BuiltIn"): the token must be known to the token
/// service (PA_ACCESS_TOKEN or memory), not expired, and issued for this FHIR server (aud).
/// </summary>
public class BuiltInTokenValidator : IAccessTokenValidator
{
    private readonly ITokenService _tokens;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public BuiltInTokenValidator(ITokenService tokens, IOptions<FhirServerSettings> server)
    {
        _tokens = tokens;
        _server = server.Value;
    }

    /// <inheritdoc />
    public async Task<TokenGrant?> ValidateAsync(string token, CancellationToken ct)
    {
        TokenGrant? grant = await _tokens.ValidateAsync(token, ct);
        if (grant == null)
        {
            return null;
        }
        if (!string.Equals(grant.Audience.TrimEnd('/'), _server.BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return null;   // issued for another FHIR server
        }
        return grant;
    }
}
