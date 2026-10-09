using System.Text.Json;

namespace MockEhr.Api.Models;

/// <summary>
/// One answer of a DTR form, flattened so the EHR can show it without reading FHIR.
/// Plain JSON (camelCase), not FHIR.
/// </summary>
public class QuestionnaireAnswerDto
{
    /// <summary>Id of the question in the payer's questionnaire.</summary>
    /// <example>egfr</example>
    public string LinkId { get; set; } = string.Empty;

    /// <summary>Text of the question.</summary>
    /// <example>Most recent eGFR (mL/min/1.73 m2)</example>
    public string? QuestionText { get; set; }

    /// <summary>Type of value: boolean | integer | decimal | date | datetime | string | coding.</summary>
    /// <example>decimal</example>
    public string ValueType { get; set; } = "string";

    /// <summary>The answer. Its JSON type follows valueType: true / false, a number, a text, or for coding an object { "code": "N18.6", "system": "http://hl7.org/fhir/sid/icd-10-cm", "display": "End stage renal disease" }.</summary>
    /// <example>9</example>
    public JsonElement? Value { get; set; }

    /// <summary>How the answer was given: auto (filled from EHR data) | override (filled, then changed by the user) | manual (typed by the user).</summary>
    /// <example>manual</example>
    public string? Origin { get; set; }

    /// <summary>Clinician who gave the answer, when known.</summary>
    /// <example>prac-1</example>
    public string? AnsweredByPractitionerId { get; set; }
}
