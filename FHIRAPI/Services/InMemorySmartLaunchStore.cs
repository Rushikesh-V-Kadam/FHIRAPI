using System.Collections.Concurrent;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// In-memory launches and codes. Both live only a few minutes, so memory is enough for one server instance.
/// With several instances, move to a shared store (Oracle table or Redis).
/// </summary>
public class InMemorySmartLaunchStore : ISmartLaunchStore
{
    private readonly ConcurrentDictionary<string, SmartLaunch> _launches = new();
    private readonly ConcurrentDictionary<string, AuthorizationCode> _codes = new();

    /// <inheritdoc />
    public void SaveLaunch(SmartLaunch launch) => _launches[launch.LaunchId] = launch;

    /// <inheritdoc />
    public SmartLaunch? GetLaunch(string launchId)
    {
        if (_launches.TryGetValue(launchId, out var launch) && launch.ExpiresAt > DateTimeOffset.UtcNow)
            return launch;
        return null;
    }

    /// <inheritdoc />
    public void SaveCode(AuthorizationCode code) => _codes[code.Code] = code;

    /// <inheritdoc />
    public AuthorizationCode? TakeCode(string code)
    {
        if (_codes.TryRemove(code, out var found) && found.ExpiresAt > DateTimeOffset.UtcNow)
            return found;
        return null;
    }
}
