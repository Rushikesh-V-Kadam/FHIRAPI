using System.Collections.Concurrent;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// In-memory launches and codes (Storage:SmartStore = "InMemory"). Both live only a few minutes, so memory is enough
/// for one server instance. With several instances use "EhrApi" (EhrApiSmartLaunchStore).
/// </summary>
public class InMemorySmartLaunchStore : ISmartLaunchStore
{
    private readonly ConcurrentDictionary<string, SmartLaunch> _launches = new ConcurrentDictionary<string, SmartLaunch>();

    /// <summary>Key = hash of the code (the code itself is not kept).</summary>
    private readonly ConcurrentDictionary<string, AuthorizationCode> _codes = new ConcurrentDictionary<string, AuthorizationCode>();

    /// <inheritdoc />
    public Task SaveLaunchAsync(SmartLaunch launch, CancellationToken ct)
    {
        RemoveExpired();
        _launches[launch.LaunchId] = launch;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SmartLaunch?> GetLaunchAsync(string launchId, CancellationToken ct)
    {
        SmartLaunch? launch;
        if (_launches.TryGetValue(launchId, out launch) && launch.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<SmartLaunch?>(launch);
        }
        return Task.FromResult<SmartLaunch?>(null);
    }

    /// <inheritdoc />
    public Task SaveCodeAsync(AuthorizationCode code, CancellationToken ct)
    {
        _codes[RandomIds.Sha256Base64Url(code.Code)] = code;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<AuthorizationCode?> TakeCodeAsync(string code, CancellationToken ct)
    {
        AuthorizationCode? found;
        if (_codes.TryRemove(RandomIds.Sha256Base64Url(code), out found) && found.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult<AuthorizationCode?>(found);
        }
        return Task.FromResult<AuthorizationCode?>(null);
    }

    /// <summary>Deletes expired launches and codes so memory does not grow forever.</summary>
    private void RemoveExpired()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (KeyValuePair<string, SmartLaunch> entry in _launches)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _launches.TryRemove(entry.Key, out _);
            }
        }
        foreach (KeyValuePair<string, AuthorizationCode> entry in _codes)
        {
            if (entry.Value.ExpiresAt <= now)
            {
                _codes.TryRemove(entry.Key, out _);
            }
        }
    }
}
