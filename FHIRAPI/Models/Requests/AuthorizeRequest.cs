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

    /// <summary>Required. The app's registered client id (table PA_SMART_CLIENT).</summary>
    /// <example>acme-dtr-app</example>
    [ModelBinder(Name = "client_id")]
    public string? ClientId { get; set; }

    /// <summary>Required. Where the code is sent: must be one of the app's registered redirect URIs (exact match).</summary>
    /// <example>http://localhost:5300/callback</example>
    [ModelBinder(Name = "redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>Required. Launch id the app received from the EHR launch (the "launch" URL parameter; created by POST /internal/smart-launches).</summary>
    /// <example>6-jM9cmaBKMd4rFPAl-hQp_D</example>
    [ModelBinder(Name = "launch")]
    public string? Launch { get; set; }

    /// <summary>Required. Requested scopes, separated by a space. Must contain "launch" (EHR launch).</summary>
    /// <example>launch fhirUser patient/*.rs patient/QuestionnaireResponse.cu</example>
    [ModelBinder(Name = "scope")]
    public string? Scope { get; set; }

    /// <summary>Required. Random value from the app, returned unchanged with the code.</summary>
    /// <example>af0ifjsldkj</example>
    [ModelBinder(Name = "state")]
    public string? State { get; set; }

    /// <summary>Required. FHIR base URL the app wants to call; must be this API's base URL (the "iss" of the launch).</summary>
    /// <example>http://localhost:5100/fhir/r4</example>
    [ModelBinder(Name = "aud")]
    public string? Aud { get; set; }

    /// <summary>Required. PKCE: base64url(SHA-256(code_verifier)).</summary>
    /// <example>E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM</example>
    [ModelBinder(Name = "code_challenge")]
    public string? CodeChallenge { get; set; }

    /// <summary>Required. Must be "S256" ("plain" is not allowed by SMART).</summary>
    /// <example>S256</example>
    [ModelBinder(Name = "code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }
}
