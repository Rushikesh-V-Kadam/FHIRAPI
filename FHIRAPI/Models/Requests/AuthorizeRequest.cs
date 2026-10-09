using Microsoft.AspNetCore.Mvc;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Parameters of the SMART / OAuth authorize request: query string (GET) or form post (POST, "authorize-post").
/// </summary>
public class AuthorizeRequest
{
    /// <summary>Required. Must be "code".</summary>
    /// <example>code</example>
    [ModelBinder(Name = "response_type")]
    public string? ResponseType { get; set; }

    /// <summary>The app's client id (not checked).</summary>
    /// <example>dtr-app</example>
    [ModelBinder(Name = "client_id")]
    public string? ClientId { get; set; }

    /// <summary>Required. Where the code is sent: the app's redirect URL.</summary>
    /// <example>http://localhost:5174/callback</example>
    [ModelBinder(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>Required. Launch id the app received from the EHR launch (the "launch" URL parameter; created by POST /internal/smart-launches).</summary>
    /// <example>6-jM9cmaBKMd4rFPAl-hQp_D</example>
    [ModelBinder(Name = "launch")]
    public string? Launch { get; set; }

    /// <summary>Requested scopes, separated by a space. Empty = "launch patient/*.rs patient/QuestionnaireResponse.cu".</summary>
    /// <example>launch openid fhirUser patient/*.rs patient/QuestionnaireResponse.cu</example>
    [ModelBinder(Name = "scope")]
    public string? Scope { get; set; }

    /// <summary>Random value from the app, returned unchanged with the code.</summary>
    /// <example>af0ifjsldkj</example>
    [ModelBinder(Name = "state")]
    public string? State { get; set; }



}
