namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One payer = one row of table PA_PAYER. Returned by GET api/prior-auth-data/payers.
/// The EHR team maintains these rows; the Payer Gateway only reads them (at start-up and every few minutes).
/// One JSON property = one column: payerName = PAYER_NAME, baseUrl = BASE_URL.
/// Only payerName and baseUrl must be filled. Every other property may be null or left out; the gateway then uses
/// its default. The payer's own id and the member id are not kept here: they come from the patient's insurance
/// (GET api/patients/{id}/insurances: payerId and memberId).
/// </summary>
public class PayerRow
{
    /// <summary>Key. The name the EHR sends as "payerName" in every gateway request (upper / lower case does not matter). Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>Friendly name for messages and logs. Column DISPLAY_NAME, VARCHAR2(200).</summary>
    /// <example>Mock Da Vinci Payer</example>
    public string? DisplayName { get; set; }

    /// <summary>false = configured but switched off (the gateway answers "payer not configured"). Empty = true. Column ENABLED, NUMBER(1): 1 = true, 0 = false.</summary>
    /// <example>true</example>
    public bool? Enabled { get; set; }

    /// <summary>Payer base URL. Column BASE_URL, VARCHAR2(500).</summary>
    /// <example>http://localhost:5080</example>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>CDS Hooks path under baseUrl (CRD). Empty = "cds-services"; "/" = no extra path (the payer's CDS Hooks address is baseUrl itself). Column CDS_SERVICES_PATH, VARCHAR2(200).</summary>
    /// <example>cds-services</example>
    public string? CdsServicesPath { get; set; }

    /// <summary>FHIR path under baseUrl (DTR, PAS, CDex). Empty = "fhir"; "/" = no extra path (the payer's FHIR address is baseUrl itself). Column FHIR_PATH, VARCHAR2(200).</summary>
    /// <example>fhir</example>
    public string? FhirPath { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): none | client-secret | smart-backend. Empty = none. Column AUTH_TYPE, VARCHAR2(20).</summary>
    /// <example>none</example>
    public string? AuthType { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): the payer's OAuth token URL. Column AUTH_TOKEN_URL, VARCHAR2(500).</summary>
    /// <example>https://auth.payer.example.org/oauth2/token</example>
    public string? AuthTokenUrl { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): client id registered with the payer. Column AUTH_CLIENT_ID, VARCHAR2(200).</summary>
    /// <example>provider-gateway</example>
    public string? AuthClientId { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): client secret. SENSITIVE. Column AUTH_CLIENT_SECRET, VARCHAR2(500).</summary>
    /// <example></example>
    public string? AuthClientSecret { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): OAuth scopes. Column AUTH_SCOPE, VARCHAR2(500).</summary>
    /// <example>system/*.rs</example>
    public string? AuthScope { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): true = sign each CDS Hooks call. Column SIGN_CDS_HOOKS_REQUESTS, NUMBER(1).</summary>
    /// <example>true</example>
    public bool? SignCdsHooksRequests { get; set; }

    /// <summary>true = the gateway asks the payer to notify it about decisions (normally true). Empty = true. Column USE_SUBSCRIPTIONS, NUMBER(1).</summary>
    /// <example>true</example>
    public bool? UseSubscriptions { get; set; }

    /// <summary>Not read by the gateway (kept for the authentication layer): secret the payer sends with notifications. SENSITIVE. Column NOTIFICATION_SECRET, VARCHAR2(200).</summary>
    /// <example></example>
    public string? NotificationSecret { get; set; }

    /// <summary>Seconds to wait for the payer at order-sign. Empty or 0 = 5. Column CRD_TIMEOUT_SECONDS, NUMBER(4).</summary>
    /// <example>10</example>
    public int? CrdTimeoutSeconds { get; set; }

    /// <summary>Seconds to wait for every other payer call. Empty or 0 = 30. Column TIMEOUT_SECONDS, NUMBER(4).</summary>
    /// <example>30</example>
    public int? TimeoutSeconds { get; set; }

    /// <summary>R4 | R5: FHIR release the payer works in. Empty = R4. Column FHIR_VERSION, VARCHAR2(5).</summary>
    /// <example>R4</example>
    public string? FhirVersion { get; set; }

    /// <summary>Forms (the EHR shows the payer's questions) | SmartApp (the payer's own SMART DTR app). Empty = Forms. Column DTR_MODE, VARCHAR2(10).</summary>
    /// <example>Forms</example>
    public string? DtrMode { get; set; }

    /// <summary>Launch URL of the payer's SMART DTR app. Empty = the URL on the payer's CRD card. Column DTR_APP_LAUNCH_URL, VARCHAR2(500).</summary>
    /// <example>https://dtr.payer.example.org/launch</example>
    public string? DtrAppLaunchUrl { get; set; }
}
