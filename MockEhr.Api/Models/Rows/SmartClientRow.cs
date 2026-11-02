using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One registered SMART app (a payer's DTR app) = one row of table PA_SMART_CLIENT, maintained by the EHR team.
/// Returned by GET api/prior-auth-data/smart-clients/{clientId}; the FHIR API reads it at every SMART authorize.
/// </summary>
public class SmartClientRow
{
    /// <summary>Key. The app's client_id, given to the payer when the app is registered. Column CLIENT_ID, VARCHAR2(100).</summary>
    /// <example>mock-payer-dtr-app</example>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Payer that owns the app (= PA_PAYER.INSURANCE_PLAN_ID). The app may only use launches of this payer's DTR links.
    /// Empty = any launch. Column INSURANCE_PLAN_ID, NUMBER(10).
    /// </summary>
    /// <example>1001</example>
    public int? InsurancePlanId { get; set; }

    /// <summary>Name shown in log lines. Column CLIENT_NAME, VARCHAR2(200).</summary>
    /// <example>Mock payer DTR app</example>
    public string? ClientName { get; set; }

    /// <summary>false = the app is blocked. Column ENABLED, NUMBER(1).</summary>
    /// <example>true</example>
    [Required]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// JSON text: array of the app's redirect URIs. The authorize request must send one of them exactly.
    /// Column REDIRECT_URIS_JSON, CLOB.
    /// </summary>
    /// <example>["http://localhost:5300/callback"]</example>
    public string? RedirectUrisJson { get; set; }

    /// <summary>Scopes the app may get, separated by a space (exact strings). Empty = any scope it asks for. Column ALLOWED_SCOPES, VARCHAR2(2000).</summary>
    /// <example>launch fhirUser patient/*.rs patient/QuestionnaireResponse.cu</example>
    public string? AllowedScopes { get; set; }
}
