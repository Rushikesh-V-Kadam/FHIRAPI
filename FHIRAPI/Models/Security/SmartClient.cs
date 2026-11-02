namespace FHIRAPI.Models.Security;

/// <summary>
/// A registered SMART app (for now: a payer's DTR app). Row of table PA_SMART_CLIENT, or Security:LocalClients.
/// Only public apps are supported (browser apps without a secret, protected by PKCE).
/// TODO: confidential apps (client secret or private_key_jwt) are not supported yet; add them when a payer needs it.
/// </summary>
public class SmartClient
{
    /// <summary>The app's client_id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Payer that owns the app (EHR insurance plan id). Null = the app may use any launch.</summary>
    public int? InsurancePlanId { get; set; }

    /// <summary>Name shown in log lines.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>false = the app is blocked.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Allowed redirect_uri values; the authorize request must send one of them exactly.</summary>
    public List<string> RedirectUris { get; set; } = new List<string>();

    /// <summary>Scopes the app may get, separated by a space (exact strings). Null or empty = any scope it asks for.</summary>
    public string? AllowedScopes { get; set; }
}
