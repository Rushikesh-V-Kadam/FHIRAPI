using Microsoft.AspNetCore.Mvc;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Form fields of the token request (application/x-www-form-urlencoded).
/// </summary>
public class TokenRequest
{
    /// <summary>Required. Must be "authorization_code".</summary>
    /// <example>authorization_code</example>
    [ModelBinder(Name = "grant_type")]
    public string? GrantType { get; set; }

    /// <summary>Required. The code from authorize. It works once only.</summary>
    /// <example>Jx3n0Yd2kQ8mVw5ZrT1uLb7c</example>
    [ModelBinder(Name = "code")]
    public string? Code { get; set; }

    /// <summary>Required. The same redirect_uri as in authorize.</summary>
    /// <example>http://localhost:5300/callback</example>
    [ModelBinder(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>Required (public apps). The same client_id as in authorize.</summary>
    /// <example>acme-dtr-app</example>
    [ModelBinder(Name = "client_id")]
    public string? ClientId { get; set; }

    /// <summary>Required. PKCE: the secret whose SHA-256 was sent as code_challenge.</summary>
    /// <example>dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk</example>
    [ModelBinder(Name = "code_verifier")]
    public string? CodeVerifier { get; set; }
}
