using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One SMART authorization code = one row of table PA_SMART_AUTH_CODE. Saved by the FHIR API at authorize and taken
/// (DELETE returns the row and deletes it) at token, so a code works once only. Lives 2 minutes.
/// </summary>
public class SmartAuthCodeRow
{
    /// <summary>Key. base64url(SHA-256(code)); the code itself is never stored. Column CODE_HASH, VARCHAR2(64).</summary>
    /// <example>3q2-7wQx0p1mZQ8u0pX6m0nN4l1qkE6m4X0dJ0hKc7Y</example>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>App the code was issued to (= PA_SMART_CLIENT.CLIENT_ID). Column CLIENT_ID, VARCHAR2(100).</summary>
    /// <example>mock-payer-dtr-app</example>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>redirect_uri of the authorize request. Column REDIRECT_URI, VARCHAR2(1000).</summary>
    /// <example>http://localhost:5300/callback</example>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Granted scopes, space separated. Column SCOPE, VARCHAR2(2000).</summary>
    /// <example>launch patient/*.rs patient/QuestionnaireResponse.cu</example>
    public string Scope { get; set; } = string.Empty;

    /// <summary>PKCE S256 code_challenge. Column CODE_CHALLENGE, VARCHAR2(128).</summary>
    /// <example>E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM</example>
    public string CodeChallenge { get; set; } = string.Empty;

    /// <summary>= PA_SMART_LAUNCH.LAUNCH_ID. Column LAUNCH_ID, VARCHAR2(64).</summary>
    /// <example>6-jM9cmaBKMd4rFPAl-hQp_D</example>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>UTC, ISO-8601 with Z. Column EXPIRES_AT, TIMESTAMP.</summary>
    /// <example>2026-10-31T10:02:00Z</example>
    [Required]
    public DateTime ExpiresAt { get; set; }
}
