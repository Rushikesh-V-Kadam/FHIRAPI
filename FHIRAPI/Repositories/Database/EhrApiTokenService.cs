using FHIRAPI.Helpers;
using FHIRAPI.Models.Security;
using FHIRAPI.Repositories.Database.Rows;
using FHIRAPI.Services;

namespace FHIRAPI.Repositories.Database;

/// <summary>
/// Access tokens in the EHR database (Storage:SmartStore = "EhrApi"), so every server instance can check them:
///   PA_ACCESS_TOKEN   GET / PUT access-tokens/{tokenHash}
/// Only a hash of the token is stored. Each checked FHIR call reads one row by its primary key.
/// </summary>
public class EhrApiTokenService : ITokenService
{
    private readonly EhrDataApi _api;

    /// <summary>Created by dependency injection.</summary>
    public EhrApiTokenService(EhrDataApi api)
    {
        _api = api;
    }

    /// <inheritdoc />
    public async Task<string> IssueAsync(TokenGrant grant, CancellationToken ct)
    {
        string token = RandomIds.New(32);
        AccessTokenRow row = new AccessTokenRow();
        row.TokenHash = RandomIds.Sha256Base64Url(token);
        row.Subject = grant.Subject;
        row.Scope = grant.Scope;
        row.PatientId = grant.PatientId;
        row.Audience = grant.Audience;
        row.ExpiresAt = grant.ExpiresAt.UtcDateTime;
        await _api.PutAsync("access-tokens/" + Uri.EscapeDataString(row.TokenHash), row, ct);
        return token;
    }

    /// <inheritdoc />
    public async Task<TokenGrant?> ValidateAsync(string token, CancellationToken ct)
    {
        string hash = RandomIds.Sha256Base64Url(token);
        AccessTokenRow? row = await _api.GetAsync<AccessTokenRow>("access-tokens/" + Uri.EscapeDataString(hash), ct);
        if (row == null)
        {
            return null;
        }

        TokenGrant grant = new TokenGrant();
        grant.Subject = row.Subject;
        grant.Scope = row.Scope;
        grant.PatientId = row.PatientId;
        grant.Audience = row.Audience;
        grant.ExpiresAt = new DateTimeOffset(DateTime.SpecifyKind(row.ExpiresAt, DateTimeKind.Utc));
        if (!grant.IsValid)
        {
            return null;
        }
        return grant;
    }
}
