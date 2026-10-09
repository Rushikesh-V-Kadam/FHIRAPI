namespace FHIRAPI.Models.Ehr;

/// <summary>
/// DTR form written back to the EHR: POST/PUT api/patients/{patientId}/questionnaire-responses.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrQuestionnaireResponseDto
{
    public string ResponseId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? OrderId { get; set; }
    public string? CoverageId { get; set; }
    public string QuestionnaireUrl { get; set; } = string.Empty;
    public string? QuestionnaireVersion { get; set; }
    public string Status { get; set; } = "completed";
    public string? AuthoredDateTime { get; set; }
    public string? AuthorPractitionerId { get; set; }
    public List<EhrQuestionnaireAnswerDto> Answers { get; set; } = new();
    /// <summary>The complete FHIR QuestionnaireResponse, stored unchanged by the EHR.</summary>
    public string? FhirJson { get; set; }
}
