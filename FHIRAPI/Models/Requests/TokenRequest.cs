using Microsoft.AspNetCore.Mvc;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Form fields of the token request (application/x-www-form-urlencoded). Only the code is used.
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

    /// <summary>The same redirect_uri as in authorize (not checked).</summary>
    /// <example>http://localhost:5174/callback</example>
    [ModelBinder(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }





}
