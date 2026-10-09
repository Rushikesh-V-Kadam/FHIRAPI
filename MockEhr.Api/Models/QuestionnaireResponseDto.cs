namespace MockEhr.Api.Models;

/// <summary>
/// A filled-in DTR form as the FHIR API sends it to the EHR chart (readable copy):
/// POST api/patients/{patientId}/questionnaire-responses and PUT .../questionnaire-responses/{responseId}.
/// Plain JSON (camelCase), not FHIR. The EHR answers with the same object and the responseId it gave it.
/// </summary>
public class QuestionnaireResponseDto
{
    /// <summary>Id the EHR gives its copy of the form. Empty in a POST (the EHR creates it); the same value as in the URL in a PUT.</summary>
    /// <example>qr-1</example>
    public string ResponseId { get; set; } = string.Empty;

    /// <summary>Patient the form is about (the same value as in the URL).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Order the form was filled for.</summary>
    /// <example>ord-1-hd</example>
    public string? OrderId { get; set; }

    /// <summary>Insurance the form was filled for.</summary>
    /// <example>cov-1</example>
    public string? CoverageId { get; set; }

    /// <summary>Canonical URL of the payer's questionnaire.</summary>
    /// <example>http://example.org/fhir/Questionnaire/dialysis-incenter-hd</example>
    public string QuestionnaireUrl { get; set; } = string.Empty;

    /// <summary>Version of the questionnaire.</summary>
    /// <example>1.0.0</example>
    public string? QuestionnaireVersion { get; set; }

    /// <summary>in-progress | completed | amended | entered-in-error | stopped.</summary>
    /// <example>completed</example>
    public string Status { get; set; } = "completed";

    /// <summary>When the form was filled in (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-10-07T18:36:05Z</example>
    public string? AuthoredDateTime { get; set; }

    /// <summary>Clinician who filled it in.</summary>
    /// <example>prac-1</example>
    public string? AuthorPractitionerId { get; set; }

    /// <summary>The answers, one entry per answered question.</summary>
    public List<QuestionnaireAnswerDto> Answers { get; set; } = new();

    /// <summary>The complete FHIR QuestionnaireResponse, stored unchanged by the EHR.</summary>
    /// <example>{"resourceType":"QuestionnaireResponse","id":"qr-frm-235b7ad69e85","status":"completed","subject":{"reference":"Patient/pat-1"},"item":[{"linkId":"egfr","answer":[{"valueDecimal":9}]}]}</example>
    public string? FhirJson { get; set; }
}
