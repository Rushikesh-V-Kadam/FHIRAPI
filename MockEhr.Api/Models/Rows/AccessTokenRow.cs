using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One access token issued by the FHIR API = one row of table PA_ACCESS_TOKEN: SMART app tokens and the CRD
/// fhirAuthorization tokens given to payers. Read on every checked FHIR call (by primary key).
/// </summary>
public class AccessTokenRow
{
    /// <summary>Key. base64url(SHA-256(token)); the token itself is never stored. Column TOKEN_HASH, VARCHAR2(64).</summary>
    /// <example>p9w1Cq8m3uYw4mV2Fh0eQk5s7rT6nB1aXz8dLc2jK4E</example>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>SMART client id or payer name. Column SUBJECT, VARCHAR2(200).</summary>
    /// <example>mock-payer-dtr-app</example>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Granted scopes, space separated. Column SCOPE, VARCHAR2(2000).</summary>
    /// <example>launch patient/*.rs patient/QuestionnaireResponse.cu</example>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The only patient the token reaches; empty = no patient limit. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string? PatientId { get; set; }

    /// <summary>FHIR base URL the token is for. Column AUDIENCE, VARCHAR2(1000).</summary>
    /// <example>http://localhost:5100/fhir/r4</example>
    public string Audience { get; set; } = string.Empty;

    /// <summary>UTC, ISO-8601 with Z. Column EXPIRES_AT, TIMESTAMP.</summary>
    /// <example>2026-10-31T11:00:00Z</example>
    [Required]
    public DateTime ExpiresAt { get; set; }
}
