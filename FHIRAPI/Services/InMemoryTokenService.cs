using System.Collections.Concurrent;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Keeps tokens in memory (hash of the token -> grant), Storage:SmartStore = "InMemory".
/// Fine for one server instance. With several instances use "EhrApi" (EhrApiTokenService).
/// </summary>
public class InMemoryTokenService : ITokenService
{
    private readonly ConcurrentDictionary<string, TokenGrant> _tokens = new ConcurrentDictionary<string, TokenGrant>();

    /// <inheritdoc />
    public Task<string> IssueAsync(TokenGrant grant, CancellationToken ct)
    {
        RemoveExpired();
        string token = RandomIds.New(32);
        _tokens[RandomIds.Sha256Base64Url(token)] = grant;
        return Task.FromResult(token);
    }

    /// <inheritdoc />
    public Task<TokenGrant?> ValidateAsync(string token, CancellationToken ct)
    {
        TokenGrant? grant;
        if (_tokens.TryGetValue(RandomIds.Sha256Base64Url(token), out grant) && grant.IsValid)
        {
            return Task.FromResult<TokenGrant?>(grant);
        }
        return Task.FromResult<TokenGrant?>(null);
    }

    /// <summary>Deletes expired tokens so memory does not grow forever.</summary>
    private void RemoveExpired()
    {
        foreach (KeyValuePair<string, TokenGrant> entry in _tokens)
        {
            if (!entry.Value.IsValid)
            {
                _tokens.TryRemove(entry.Key, out _);
            }
        }
    }
}
