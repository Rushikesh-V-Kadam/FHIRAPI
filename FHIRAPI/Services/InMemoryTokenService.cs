using System.Collections.Concurrent;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Keeps tokens in memory (opaque random strings -> grant).
/// Fine for one server instance. With several instances behind a load balancer, move this to a shared store
/// (an Oracle table or Redis) or replace it with a real OAuth server.
/// </summary>
public class InMemoryTokenService : ITokenService
{
    private readonly ConcurrentDictionary<string, TokenGrant> _tokens = new();

    /// <inheritdoc />
    public string Issue(TokenGrant grant)
    {
        RemoveExpired();
        var token = RandomIds.New(32);
        _tokens[token] = grant;
        return token;
    }

    /// <inheritdoc />
    public TokenGrant? Validate(string token)
    {
        if (_tokens.TryGetValue(token, out var grant) && grant.IsValid)
            return grant;
        return null;
    }

    /// <summary>Deletes expired tokens so memory does not grow forever.</summary>
    private void RemoveExpired()
    {
        foreach (var entry in _tokens)
        {
            if (!entry.Value.IsValid) _tokens.TryRemove(entry.Key, out _);
        }
    }
}
