using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Stores SMART launches (from /internal/smart-launches) and OAuth authorization codes (from /auth/authorize).
/// InMemorySmartLaunchStore (one server) or EhrApiSmartLaunchStore (EHR database, several servers): Storage:SmartStore.
/// </summary>
public interface ISmartLaunchStore
{
    /// <summary>Saves a new launch.</summary>
    Task SaveLaunchAsync(SmartLaunch launch, CancellationToken ct);

    /// <summary>Returns a launch that has not expired, or null.</summary>
    Task<SmartLaunch?> GetLaunchAsync(string launchId, CancellationToken ct);

    /// <summary>Saves a new authorization code (only a hash of the code is kept).</summary>
    Task SaveCodeAsync(AuthorizationCode code, CancellationToken ct);

    /// <summary>Returns and deletes a code (codes can be used only once). Null when unknown, used or expired.</summary>
    Task<AuthorizationCode?> TakeCodeAsync(string code, CancellationToken ct);
}
