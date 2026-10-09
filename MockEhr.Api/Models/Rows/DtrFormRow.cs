using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One payer questionnaire fetched for an order, with its latest answers = one row of table PA_DTR_FORM.
/// Body of PUT api/prior-auth-data/dtr-forms/{formId}; returned by GET of the same path.
/// Lists are sent as JSON text (the "...Json" properties) so they can be stored unchanged in CLOB columns.
/// </summary>
public class DtrFormRow
{
    /// <summary>Key. Created by the gateway (the same value as in the URL). Column FORM_ID, VARCHAR2(64).</summary>
    /// <example>frm-235b7ad69e85</example>
    public string FormId { get; set; } = string.Empty;

    /// <summary>Payer the questionnaire came from. Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>EHR patient id. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Insurance the form is for. Column COVERAGE_ID, VARCHAR2(64).</summary>
    /// <example>cov-1</example>
    public string? CoverageId { get; set; }

    /// <summary>JSON text: array of the order ids the form is for. Column ORDER_IDS_JSON, CLOB.</summary>
    /// <example>["ord-1-hd"]</example>
    public string? OrderIdsJson { get; set; }

    /// <summary>Canonical URL of the payer Questionnaire (may end with |version). Column QUESTIONNAIRE_URL, VARCHAR2(1000).</summary>
    /// <example>http://example.org/fhir/Questionnaire/dialysis-incenter-hd|1.0.0</example>
    public string QuestionnaireUrl { get; set; } = string.Empty;

    /// <summary>
    /// JSON text: the payer's FHIR Questionnaire (can be 100 KB or more).
    /// Store the text unchanged and return it unchanged. Column QUESTIONNAIRE_JSON, CLOB.
    /// </summary>
    /// <example>{"resourceType":"Questionnaire","id":"dialysis-incenter-hd","url":"http://example.org/fhir/Questionnaire/dialysis-incenter-hd","status":"active","item":[{"linkId":"egfr","text":"Most recent eGFR","type":"decimal"}]}</example>
    public string QuestionnaireJson { get; set; } = string.Empty;

    /// <summary>new | in-progress | completed. Column STATUS, VARCHAR2(20).</summary>
    /// <example>completed</example>
    public string Status { get; set; } = "new";

    /// <summary>Id of the filled-in form in table PA_QUESTIONNAIRE_RESPONSE. Empty until answers are saved. Column QUESTIONNAIRE_RESPONSE_ID, VARCHAR2(64).</summary>
    /// <example>qr-frm-235b7ad69e85</example>
    public string? QuestionnaireResponseId { get; set; }

    /// <summary>JSON text: array of the latest plain answers. Column ANSWERS_JSON, CLOB.</summary>
    /// <example>[{"linkId":"egfr","values":["9"]}]</example>
    public string? AnswersJson { get; set; }

    /// <summary>true = adaptive questionnaire: the payer adds questions step by step. Column ADAPTIVE, NUMBER(1).</summary>
    /// <example>false</example>
    [Required]
    public bool Adaptive { get; set; }

    /// <summary>Adaptive only: true = the payer has no more questions. Column PAYER_FINISHED, NUMBER(1).</summary>
    /// <example>false</example>
    [Required]
    public bool PayerFinished { get; set; }

    /// <summary>JSON text, adaptive only: the last FHIR QuestionnaireResponse returned by the payer. Column ADAPTIVE_RESPONSE_JSON, CLOB.</summary>
    /// <example></example>
    public string? AdaptiveResponseJson { get; set; }

    /// <summary>When the form was created (UTC, ISO-8601 with Z). Column CREATED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:35:52.4410000Z</example>
    [Required]
    public DateTime CreatedAt { get; set; }

    /// <summary>When the form was last saved (UTC, ISO-8601 with Z). Column UPDATED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:36:05.9120000Z</example>
    [Required]
    public DateTime UpdatedAt { get; set; }
}
