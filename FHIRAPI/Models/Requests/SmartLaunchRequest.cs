using System.ComponentModel.DataAnnotations;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Body of POST /internal/smart-launches (sent by the Payer Gateway when a user opens a DTR link).
/// </summary>
public class SmartLaunchRequest
{
    // [Required(AllowEmptyStrings = true)] only marks the property as required in Swagger. The controller itself
    // checks for an empty value and answers 400 with the usual { "error", "error_description" } body.

    /// <summary>Required. Patient the DTR form is for (EHR patient id).</summary>
    /// <example>pat-1</example>
    [Required(AllowEmptyStrings = true)]
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Logged-in clinician (becomes fhirUser = Practitioner/{id}).</summary>
    /// <example>prac-1</example>
    public string? UserPractitionerId { get; set; }

    /// <summary>Current encounter, if any.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>References handed to the DTR app as fhirContext: the orders and the coverage the form is for.</summary>
    /// <example>["ServiceRequest/ord-1-hd", "Coverage/cov-1"]</example>
    public List<string> FhirContext { get; set; } = new();

    /// <summary>appContext from the payer's CRD card (which questionnaires to load, coverage-assertion-id). Passed to the app unchanged.</summary>
    /// <example>{"coverage-assertion-id":"702644806bf74c70aa65d2ca1a4a34b1","questionnaire":["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"]}</example>
    public string? AppContext { get; set; }

    /// <summary>
    /// FHIR release the DTR app works with: "r4" (default) or "r5". Decides the "iss" returned, i.e. whether the app
    /// reads /fhir/r4 or /fhir/r5.
    /// </summary>
    /// <example>r4</example>
    public string? FhirVersion { get; set; }
}
