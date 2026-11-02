using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Registered SMART apps. EhrApiSmartClientStore (table PA_SMART_CLIENT) or InMemorySmartClientStore
/// (Security:LocalClients): Storage:SmartStore.
/// </summary>
public interface ISmartClientStore
{
    /// <summary>The registered app with this client_id, or null.</summary>
    Task<SmartClient?> GetClientAsync(string clientId, CancellationToken ct);
}
