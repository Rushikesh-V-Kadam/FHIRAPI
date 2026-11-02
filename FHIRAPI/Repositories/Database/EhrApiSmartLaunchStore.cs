using System.Text.Json;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Security;
using FHIRAPI.Repositories.Database.Rows;
using FHIRAPI.Services;

namespace FHIRAPI.Repositories.Database;

/// <summary>
/// SMART launches and authorization codes in the EHR database (Storage:SmartStore = "EhrApi"), so every server
/// instance sees them:
///   PA_SMART_LAUNCH      GET / PUT smart-launches/{launchId}
///   PA_SMART_AUTH_CODE   PUT smart-auth-codes/{codeHash},  DELETE smart-auth-codes/{codeHash} (take once)
/// </summary>
public class EhrApiSmartLaunchStore : ISmartLaunchStore
{
    private readonly EhrDataApi _api;

    /// <summary>Created by dependency injection.</summary>
    public EhrApiSmartLaunchStore(EhrDataApi api)
    {
        _api = api;
    }

    /// <inheritdoc />
    public async Task SaveLaunchAsync(SmartLaunch launch, CancellationToken ct)
    {
        SmartLaunchRow row = new SmartLaunchRow();
        row.LaunchId = launch.LaunchId;
        row.InsurancePlanId = launch.InsurancePlanId;
        row.PatientId = launch.PatientId;
        row.EncounterId = launch.EncounterId;
        row.UserPractitionerId = launch.UserPractitionerId;
        row.FhirContextJson = JsonSerializer.Serialize(launch.FhirContext);
        row.AppContext = launch.AppContext;
        row.ExpiresAt = launch.ExpiresAt.UtcDateTime;
        await _api.PutAsync("smart-launches/" + Uri.EscapeDataString(launch.LaunchId), row, ct);
    }

    /// <inheritdoc />
    public async Task<SmartLaunch?> GetLaunchAsync(string launchId, CancellationToken ct)
    {
        SmartLaunchRow? row = await _api.GetAsync<SmartLaunchRow>("smart-launches/" + Uri.EscapeDataString(launchId), ct);
        if (row == null)
        {
            return null;
        }
        DateTimeOffset expiresAt = ToUtc(row.ExpiresAt);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        SmartLaunch launch = new SmartLaunch();
        launch.LaunchId = row.LaunchId;
        launch.InsurancePlanId = row.InsurancePlanId;
        launch.PatientId = row.PatientId;
        launch.EncounterId = row.EncounterId;
        launch.UserPractitionerId = row.UserPractitionerId;
        launch.FhirContext = ReadList(row.FhirContextJson, "PA_SMART_LAUNCH.FHIR_CONTEXT_JSON");
        launch.AppContext = row.AppContext;
        launch.ExpiresAt = expiresAt;
        return launch;
    }

    /// <inheritdoc />
    public async Task SaveCodeAsync(AuthorizationCode code, CancellationToken ct)
    {
        SmartAuthCodeRow row = new SmartAuthCodeRow();
        row.CodeHash = RandomIds.Sha256Base64Url(code.Code);
        row.ClientId = code.ClientId;
        row.RedirectUri = code.RedirectUri;
        row.Scope = code.Scope;
        row.CodeChallenge = code.CodeChallenge;
        row.LaunchId = code.LaunchId;
        row.ExpiresAt = code.ExpiresAt.UtcDateTime;
        await _api.PutAsync("smart-auth-codes/" + Uri.EscapeDataString(row.CodeHash), row, ct);
    }

    /// <inheritdoc />
    public async Task<AuthorizationCode?> TakeCodeAsync(string code, CancellationToken ct)
    {
        string hash = RandomIds.Sha256Base64Url(code);
        // DELETE returns the row and removes it in one step, so two requests can never use the same code
        SmartAuthCodeRow? row = await _api.DeleteAsync<SmartAuthCodeRow>("smart-auth-codes/" + Uri.EscapeDataString(hash), ct);
        if (row == null)
        {
            return null;
        }
        DateTimeOffset expiresAt = ToUtc(row.ExpiresAt);
        if (expiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        AuthorizationCode found = new AuthorizationCode();
        found.Code = code;
        found.ClientId = row.ClientId;
        found.RedirectUri = row.RedirectUri;
        found.Scope = row.Scope;
        found.CodeChallenge = row.CodeChallenge;
        found.LaunchId = row.LaunchId;
        found.ExpiresAt = expiresAt;
        return found;
    }

    /// <summary>A UTC column value (JSON with Z) as DateTimeOffset.</summary>
    private static DateTimeOffset ToUtc(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    /// <summary>JSON array text -> list; empty -> empty list; anything else -> EhrException.</summary>
    private static List<string> ReadList(string? json, string column)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }
        try
        {
            List<string>? list = JsonSerializer.Deserialize<List<string>>(json);
            if (list == null)
            {
                return new List<string>();
            }
            return list;
        }
        catch (JsonException ex)
        {
            throw new EhrException(column + " is not a JSON array of text: " + ex.Message, ex);
        }
    }
}
