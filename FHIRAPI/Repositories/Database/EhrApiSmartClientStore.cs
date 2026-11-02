using System.Text.Json;
using FHIRAPI.Exceptions;
using FHIRAPI.Models.Security;
using FHIRAPI.Repositories.Database.Rows;
using FHIRAPI.Services;

namespace FHIRAPI.Repositories.Database;

/// <summary>
/// Registered SMART apps from the EHR database (Storage:SmartStore = "EhrApi"):
///   PA_SMART_CLIENT   GET smart-clients/{clientId}   (read only: the EHR team maintains the table)
/// To register a payer's DTR app: insert a row. Nothing changes in this API.
/// </summary>
public class EhrApiSmartClientStore : ISmartClientStore
{
    private readonly EhrDataApi _api;

    /// <summary>Created by dependency injection.</summary>
    public EhrApiSmartClientStore(EhrDataApi api)
    {
        _api = api;
    }

    /// <inheritdoc />
    public async Task<SmartClient?> GetClientAsync(string clientId, CancellationToken ct)
    {
        SmartClientRow? row = await _api.GetAsync<SmartClientRow>("smart-clients/" + Uri.EscapeDataString(clientId), ct);
        if (row == null)
        {
            return null;
        }

        SmartClient client = new SmartClient();
        client.ClientId = row.ClientId;
        client.InsurancePlanId = row.InsurancePlanId;
        client.ClientName = row.ClientName ?? row.ClientId;
        client.Enabled = row.Enabled;
        client.RedirectUris = ReadUris(row.RedirectUrisJson);
        client.AllowedScopes = row.AllowedScopes;
        return client;
    }

    /// <summary>REDIRECT_URIS_JSON -> list; empty -> empty list (then no redirect_uri is accepted).</summary>
    private static List<string> ReadUris(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }
        try
        {
            List<string>? uris = JsonSerializer.Deserialize<List<string>>(json);
            if (uris == null)
            {
                return new List<string>();
            }
            return uris;
        }
        catch (JsonException ex)
        {
            throw new EhrException("PA_SMART_CLIENT.REDIRECT_URIS_JSON is not a JSON array of text: " + ex.Message, ex);
        }
    }
}
