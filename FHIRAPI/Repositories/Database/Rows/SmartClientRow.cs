namespace FHIRAPI.Repositories.Database.Rows;

/// <summary>
/// One registered SMART app = one row of table PA_SMART_CLIENT (maintained by the EHR team).
/// Returned by GET {Ehr:DataPath}/smart-clients/{clientId}.
/// </summary>
public class SmartClientRow
{
    /// <summary>Key. The app's client_id. Column CLIENT_ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Payer that owns the app (= PA_PAYER.INSURANCE_PLAN_ID); null = any launch. Column INSURANCE_PLAN_ID.</summary>
    public int? InsurancePlanId { get; set; }

    /// <summary>Name shown in log lines. Column CLIENT_NAME.</summary>
    public string? ClientName { get; set; }

    /// <summary>false = the app is blocked. Column ENABLED (1/0).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>JSON text: array of allowed redirect URIs, e.g. ["https://app.payer.example/callback"]. Column REDIRECT_URIS_JSON.</summary>
    public string? RedirectUrisJson { get; set; }

    /// <summary>Scopes the app may get, space separated; empty = any. Column ALLOWED_SCOPES.</summary>
    public string? AllowedScopes { get; set; }
}
