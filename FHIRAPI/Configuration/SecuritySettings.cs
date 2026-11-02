namespace FHIRAPI.Configuration;

/// <summary>
/// Access checks of this API (appsettings section "Security").
/// </summary>
public class SecuritySettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "Security";

    /// <summary>
    /// true = every /fhir/r4 call (except metadata, .well-known/smart-configuration, auth/authorize and auth/token) must
    /// send "Authorization: Bearer {token}" with a token issued by this API (auth/token or /internal/access-tokens),
    /// or the internal API key. The token must not be expired, must be for this server (aud) and its scopes must allow
    /// the call; a patient token only reaches its own patient's data.
    /// false = no check (use this when the checks are done in front of this API, e.g. by an API gateway).
    /// </summary>
    public bool ValidateAccessTokens { get; set; } = true;

    /// <summary>
    /// Key the Payer Gateway sends in the X-Api-Key header. Required on /internal/* when set. On /fhir/r4 it gives
    /// full access without a token (the gateway reads data for many patients).
    /// Empty = /internal/* is open and /fhir/r4 always needs a token. Keep it secret (encrypted settings file).
    /// </summary>
    public string InternalApiKey { get; set; } = string.Empty;

    /// <summary>Lifetime of the access token given to a SMART app by auth/token, in seconds (SMART: at most one hour).</summary>
    public int SmartTokenSeconds { get; set; } = 3600;

    /// <summary>
    /// SMART apps used when Storage:SmartStore is "InMemory" (local testing without the EHR data API).
    /// With "EhrApi" the apps are read from the EHR database (table PA_SMART_CLIENT) and this list is not used.
    /// </summary>
    public List<SmartClientSetting> LocalClients { get; set; } = new List<SmartClientSetting>();
}

/// <summary>One registered SMART app in appsettings (Security:LocalClients), for local testing only.</summary>
public class SmartClientSetting
{
    /// <summary>The app's client_id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Payer that owns the app (EHR insurance plan id). Empty = the app may use any launch.</summary>
    public int? InsurancePlanId { get; set; }

    /// <summary>Name shown in log lines.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>false = the app is blocked.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Allowed redirect_uri values (exact match).</summary>
    public List<string> RedirectUris { get; set; } = new List<string>();

    /// <summary>Scopes the app may get, separated by a space (exact strings). Empty = any scope it asks for.</summary>
    public string? AllowedScopes { get; set; }
}
