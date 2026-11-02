using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Response of GET /internal/smart-launches/{launchId}/context: the SMART launch context the Keycloak SMART launch
/// add-on puts into the token response (patient, encounter, fhirUser, fhirContext, appContext) and into the access token
/// (patient). Property names are the SMART token response names, so the add-on copies them unchanged.
/// </summary>
public class SmartLaunchContextResponse
{
    /// <summary>Patient in context (EHR patient id).</summary>
    /// <example>pat-1</example>
    [JsonPropertyName("patient")]
    public string Patient { get; set; } = string.Empty;

    /// <summary>Encounter in context, if any.</summary>
    /// <example>enc-1</example>
    [JsonPropertyName("encounter")]
    public string? Encounter { get; set; }

    /// <summary>Clinician who opened the link, e.g. "Practitioner/prac-1"; null when the EHR did not send one.</summary>
    /// <example>Practitioner/prac-1</example>
    [JsonPropertyName("fhirUser")]
    public string? FhirUser { get; set; }

    /// <summary>Orders and coverage the form is for: [{ "reference": "ServiceRequest/ord-1-hd" }].</summary>
    [JsonPropertyName("fhirContext")]
    public List<FhirContextItem> FhirContext { get; set; } = new();

    /// <summary>appContext from the payer's CRD card (passed to the app unchanged).</summary>
    /// <example>{"coverage-assertion-id":"702644806bf74c70aa65d2ca1a4a34b1","questionnaire":["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"]}</example>
    [JsonPropertyName("appContext")]
    public string? AppContext { get; set; }

    /// <summary>Always false: the app must show its own patient banner.</summary>
    /// <example>false</example>
    [JsonPropertyName("need_patient_banner")]
    public bool NeedPatientBanner { get; set; }
}
