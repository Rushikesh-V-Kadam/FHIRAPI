using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR QuestionnaireResponse: a DTR form filled in by the DTR app.
/// Only the fields this API reads are modelled. The complete JSON sent by the app is stored unchanged
/// (see QuestionnaireResponseService), so nothing is lost by the missing fields here.
/// </summary>
public class QuestionnaireResponse : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "QuestionnaireResponse";

    /// <summary>Canonical URL of the Questionnaire, optionally "|version".</summary>
    public string? Questionnaire { get; set; }

    /// <summary>in-progress | completed | amended | entered-in-error | stopped</summary>
    public string Status { get; set; } = "in-progress";

    /// <summary>The patient ("Patient/id"). Required.</summary>
    public ResourceReference? Subject { get; set; }

    public string? Authored { get; set; }
    public ResourceReference? Author { get; set; }
    public List<QuestionnaireResponseItem> Item { get; set; } = new();
}
