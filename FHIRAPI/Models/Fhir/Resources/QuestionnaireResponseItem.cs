namespace FHIRAPI.Models.Fhir;

/// <summary>
/// One question (or group of questions) in a QuestionnaireResponse.
/// </summary>
public class QuestionnaireResponseItem
{
    /// <summary>Matches Questionnaire.item.linkId.</summary>
    public string LinkId { get; set; } = string.Empty;
    public string? Text { get; set; }
    public List<QuestionnaireResponseAnswer> Answer { get; set; } = new();
    /// <summary>Child questions (groups).</summary>
    public List<QuestionnaireResponseItem> Item { get; set; } = new();
}
