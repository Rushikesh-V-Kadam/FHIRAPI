using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Response of POST /fhir/r4/auth/token: the access token plus the SMART launch context.
/// </summary>
public class SmartTokenResponse
{
    /// <summary>A random value. No endpoint of this API checks it.</summary>
    /// <example>p0Qm3s8dYw1cV7r2Kx9TbA4nL6eJ5hGf</example>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Always "Bearer".</summary>
    /// <example>Bearer</example>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Lifetime reported to the app, in seconds.</summary>
    /// <example>3600</example>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>The scopes the app asked for (or the default DTR scopes).</summary>
    /// <example>launch patient/*.rs patient/QuestionnaireResponse.cu</example>
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>Patient in context.</summary>
    /// <example>pat-1</example>
    [JsonPropertyName("patient")]
    public string Patient { get; set; } = string.Empty;

    /// <summary>Encounter in context, if any.</summary>
    /// <example>enc-1</example>
    [JsonPropertyName("encounter")]
    public string? Encounter { get; set; }

    /// <summary>Logged-in user, e.g. "Practitioner/prac-1".</summary>
    /// <example>Practitioner/prac-1</example>
    [JsonPropertyName("fhirUser")]
    public string? FhirUser { get; set; }

    /// <summary>Orders and coverage the form is for: [{ "reference": "ServiceRequest/ord-1-hd" }].</summary>
    [JsonPropertyName("fhirContext")]
    public List<FhirContextItem> FhirContext { get; set; } = new();

    /// <summary>appContext from the payer's CRD card.</summary>
    /// <example>{"coverage-assertion-id":"702644806bf74c70aa65d2ca1a4a34b1","questionnaire":["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"]}</example>
    [JsonPropertyName("appContext")]
    public string? AppContext { get; set; }

    /// <summary>Always false: the app must show its own patient banner.</summary>
    /// <example>false</example>
    [JsonPropertyName("need_patient_banner")]
    public bool NeedPatientBanner { get; set; }
}
