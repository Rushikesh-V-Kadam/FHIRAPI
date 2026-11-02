namespace FHIRAPI.Repositories.Database.Rows;

/// <summary>
/// One access token = one row of table PA_ACCESS_TOKEN. GET / PUT {Ehr:DataPath}/access-tokens/{tokenHash}.
/// </summary>
public class AccessTokenRow
{
    /// <summary>Key. base64url(SHA-256(token)); the token itself is never stored. Column TOKEN_HASH.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>SMART client id or payer name. Column SUBJECT.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Granted scopes. Column SCOPE.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>The only patient the token reaches; null = no patient limit. Column PATIENT_ID.</summary>
    public string? PatientId { get; set; }

    /// <summary>FHIR base URL the token is for. Column AUDIENCE.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>UTC. Column EXPIRES_AT.</summary>
    public DateTime ExpiresAt { get; set; }
}
