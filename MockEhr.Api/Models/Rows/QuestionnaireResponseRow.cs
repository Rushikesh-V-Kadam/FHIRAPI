using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One filled-in DTR form in FHIR format = one row of table PA_QUESTIONNAIRE_RESPONSE. Saved and read by the FHIR API.
/// Body of PUT api/prior-auth-data/questionnaire-responses/{id}; returned by GET of the same path and by the list.
/// The complete FHIR JSON is in resourceJson; the other columns exist for searching and tracking.
/// </summary>
public class QuestionnaireResponseRow
{
    /// <summary>Key. FHIR id of the form, QuestionnaireResponse.id (the same value as in the URL). Column ID, VARCHAR2(64).</summary>
    /// <example>qr-frm-235b7ad69e85</example>
    public string Id { get; set; } = string.Empty;

    /// <summary>Patient the form is about. The list endpoint filters on it. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Order the form was filled for. Column ORDER_ID, VARCHAR2(64).</summary>
    /// <example>ord-1-hd</example>
    public string? OrderId { get; set; }

    /// <summary>Insurance the form was filled for. Column COVERAGE_ID, VARCHAR2(64).</summary>
    /// <example>cov-1</example>
    public string? CoverageId { get; set; }

    /// <summary>Canonical URL of the payer Questionnaire the form answers. Column QUESTIONNAIRE_URL, VARCHAR2(1000).</summary>
    /// <example>http://example.org/fhir/Questionnaire/dialysis-incenter-hd</example>
    public string? QuestionnaireUrl { get; set; }

    /// <summary>in-progress | completed | amended | entered-in-error | stopped. Column STATUS, VARCHAR2(20).</summary>
    /// <example>completed</example>
    public string Status { get; set; } = "in-progress";

    /// <summary>
    /// JSON text: the complete FHIR QuestionnaireResponse. It is sent to the payer unchanged, so it must come back
    /// exactly as it was saved (do not parse or re-format it). Column RESOURCE_JSON, CLOB.
    /// </summary>
    /// <example>{"resourceType":"QuestionnaireResponse","id":"qr-frm-235b7ad69e85","questionnaire":"http://example.org/fhir/Questionnaire/dialysis-incenter-hd","status":"completed","subject":{"reference":"Patient/pat-1"},"item":[{"linkId":"egfr","answer":[{"valueDecimal":9}]}]}</example>
    public string ResourceJson { get; set; } = string.Empty;

    /// <summary>Id of the readable chart copy in the EHR, if the EHR keeps one. Column EHR_RESPONSE_ID, VARCHAR2(64).</summary>
    /// <example>qr-1</example>
    public string? EhrResponseId { get; set; }

    /// <summary>State of that chart copy: PENDING | SENT | FAILED. Column EHR_SYNC_STATUS, VARCHAR2(10).</summary>
    /// <example>SENT</example>
    public string EhrSyncStatus { get; set; } = "PENDING";

    /// <summary>When the row was last saved (UTC, ISO-8601 with Z). The list is sorted on it, newest first. Column LAST_UPDATED_UTC, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:36:05.5092373Z</example>
    [Required]
    public DateTime LastUpdatedUtc { get; set; }
}
