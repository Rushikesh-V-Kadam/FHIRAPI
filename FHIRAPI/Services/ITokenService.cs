using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Issues and validates the access tokens used on /fhir/r4 (SMART app tokens and CDS Hooks fhirAuthorization tokens).
/// InMemoryTokenService (one server) or EhrApiTokenService (EHR database, several servers): Storage:SmartStore.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a new random token for the grant and returns it (only a hash of the token is kept).</summary>
    Task<string> IssueAsync(TokenGrant grant, CancellationToken ct);

    /// <summary>Returns the grant of a token, or null when the token is unknown or expired.</summary>
    Task<TokenGrant?> ValidateAsync(string token, CancellationToken ct);
}
