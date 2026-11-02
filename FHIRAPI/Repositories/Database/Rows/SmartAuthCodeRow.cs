namespace FHIRAPI.Repositories.Database.Rows;

/// <summary>
/// One authorization code = one row of table PA_SMART_AUTH_CODE.
/// PUT {Ehr:DataPath}/smart-auth-codes/{codeHash}; DELETE of the same path takes it (returns the row and deletes it).
/// </summary>
public class SmartAuthCodeRow
{
    /// <summary>Key. base64url(SHA-256(code)); the code itself is never stored. Column CODE_HASH.</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>Column CLIENT_ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Column REDIRECT_URI.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Granted scopes. Column SCOPE.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>PKCE S256 challenge. Column CODE_CHALLENGE.</summary>
    public string CodeChallenge { get; set; } = string.Empty;

    /// <summary>= PA_SMART_LAUNCH.LAUNCH_ID. Column LAUNCH_ID.</summary>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>UTC. Column EXPIRES_AT.</summary>
    public DateTime ExpiresAt { get; set; }
}
