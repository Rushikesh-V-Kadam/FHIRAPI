using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Registered SMART apps from appsettings Security:LocalClients (Storage:SmartStore = "InMemory", local testing).
/// </summary>
public class InMemorySmartClientStore : ISmartClientStore
{
    private readonly SecuritySettings _security;

    /// <summary>Created by dependency injection.</summary>
    public InMemorySmartClientStore(IOptions<SecuritySettings> security)
    {
        _security = security.Value;
    }

    /// <inheritdoc />
    public Task<SmartClient?> GetClientAsync(string clientId, CancellationToken ct)
    {
        foreach (SmartClientSetting setting in _security.LocalClients)
        {
            if (setting.ClientId == clientId)
            {
                SmartClient client = new SmartClient();
                client.ClientId = setting.ClientId;
                client.InsurancePlanId = setting.InsurancePlanId;
                client.ClientName = setting.ClientName;
                client.Enabled = setting.Enabled;
                client.RedirectUris = setting.RedirectUris;
                client.AllowedScopes = setting.AllowedScopes;
                return Task.FromResult<SmartClient?>(client);
            }
        }
        return Task.FromResult<SmartClient?>(null);
    }
}
