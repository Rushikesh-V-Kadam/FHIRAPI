using System.Text.Json;

namespace FHIRAPI.Models.Ehr;

/// <summary>
/// One answer of a DTR form, flattened for the EHR.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrQuestionnaireAnswerDto
{
    public string LinkId { get; set; } = string.Empty;
    public string? QuestionText { get; set; }
    /// <summary>boolean | integer | decimal | date | datetime | string | coding</summary>
    public string ValueType { get; set; } = "string";
    public JsonElement? Value { get; set; }
    /// <summary>auto | override | manual</summary>
    public string? Origin { get; set; }
    public string? AnsweredByPractitionerId { get; set; }
}
