using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Stores SMART launches (from /internal/smart-launches) and OAuth authorization codes (from /auth/authorize).
/// </summary>
public interface ISmartLaunchStore
{
    /// <summary>Saves a new launch.</summary>
    void SaveLaunch(SmartLaunch launch);

    /// <summary>Returns a launch that has not expired, or null.</summary>
    SmartLaunch? GetLaunch(string launchId);

    /// <summary>Saves a new authorization code.</summary>
    void SaveCode(AuthorizationCode code);

    /// <summary>Returns and deletes a code (codes can be used only once). Null when unknown or expired.</summary>
    AuthorizationCode? TakeCode(string code);
}
