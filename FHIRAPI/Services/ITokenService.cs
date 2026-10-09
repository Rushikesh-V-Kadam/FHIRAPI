using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Issues and validates the access tokens used on /fhir/r4.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a new random token for the grant and returns it.</summary>
    string Issue(TokenGrant grant);

    /// <summary>Returns the grant of a token, or null when the token is unknown or expired.</summary>
    TokenGrant? Validate(string token);
}
