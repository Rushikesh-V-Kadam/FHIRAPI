using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// Latest CRD coverage decision of one order = one row of table PA_COVERAGE_DECISION.
/// Body of PUT api/prior-auth-data/coverage-decisions/{orderId}; returned by GET of the same path.
/// One row per order: a new decision replaces the old one. The EHR order screen reads this table.
/// </summary>
public class CoverageDecisionRow
{
    /// <summary>Key. EHR order id (the same value as in the URL). Column ORDER_ID, VARCHAR2(64).</summary>
    /// <example>ord-1-hd</example>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The order-sign event that produced this decision. Column EVENT_ID, VARCHAR2(100).</summary>
    /// <example>evt-20261007-0001</example>
    public string EventId { get; set; } = string.Empty;

    /// <summary>Payer that answered. Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string? PayerName { get; set; }

    /// <summary>EHR patient id. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>The payer's id for this answer (needed later for DTR). Column COVERAGE_ASSERTION_ID, VARCHAR2(100).</summary>
    /// <example>702644806bf74c70aa65d2ca1a4a34b1</example>
    public string? CoverageAssertionId { get; set; }

    /// <summary>covered | not-covered | conditional | unknown. Column COVERED, VARCHAR2(20).</summary>
    /// <example>covered</example>
    public string Covered { get; set; } = string.Empty;

    /// <summary>no-auth | auth-needed | satisfied | performpa | conditional. Column PRIOR_AUTH_NEEDED, VARCHAR2(20).</summary>
    /// <example>auth-needed</example>
    public string? PriorAuthNeeded { get; set; }

    /// <summary>no-doc | clinical | admin | both | conditional. Column DOCUMENTATION_NEEDED, VARCHAR2(20).</summary>
    /// <example>clinical</example>
    public string? DocumentationNeeded { get; set; }

    /// <summary>
    /// JSON text: the full decision, the same structure as one entry of "decisions" in the response of
    /// POST /api/v1/crd/order-sign (callDtr, callPas, questionnaireUrls, messages ...).
    /// Store the text unchanged and return it unchanged. Column DECISION_JSON, CLOB.
    /// </summary>
    /// <example>{"orderId":"ord-1-hd","payerName":"MockPayer","coverageId":"cov-1","covered":"covered","priorAuthNeeded":"auth-needed","documentationNeeded":"clinical","callDtr":true,"callPas":true,"questionnaireUrls":["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"],"messages":[]}</example>
    public string DecisionJson { get; set; } = string.Empty;

    /// <summary>When the decision was saved (UTC, ISO-8601 with Z). Column UPDATED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:35:40.1234567Z</example>
    [Required]
    public DateTime UpdatedAt { get; set; }
}
