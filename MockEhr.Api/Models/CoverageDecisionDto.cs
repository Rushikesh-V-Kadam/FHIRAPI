namespace MockEhr.Api.Models;

/// <summary>
/// CRD answer for one order, as the mock EHR order screen shows it (GET api/orders/{orderId}/coverage-decision).
/// It is read from the decisionJson column of table PA_COVERAGE_DECISION: the same structure as one entry of
/// "decisions" in the response of the gateway's POST /api/v1/crd/order-sign.
/// </summary>
public class CoverageDecisionDto
{
    /// <summary>EHR order id.</summary>
    /// <example>ord-1-hd</example>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>The payer's id.</summary>
    /// <example>PAYER001</example>
    public string? PayerId { get; set; }

    /// <summary>Payer name sent by the Payer Gateway.</summary>
    /// <example>MockPayer</example>
    public string? PayerName { get; set; }

    /// <summary>true = fill in the payer questionnaire (DTR).</summary>
    /// <example>true</example>
    public bool CallDtr { get; set; }

    /// <summary>true = submit a prior authorization (PAS).</summary>
    /// <example>true</example>
    public bool CallPas { get; set; }

    /// <summary>Insurance the decision is for.</summary>
    /// <example>cov-1</example>
    public string? CoverageId { get; set; }

    /// <summary>covered | not-covered | conditional | unknown.</summary>
    /// <example>covered</example>
    public string Covered { get; set; } = "covered";

    /// <summary>no-auth | auth-needed | satisfied | performpa | conditional.</summary>
    /// <example>auth-needed</example>
    public string? PriorAuthNeeded { get; set; }

    /// <summary>no-doc | clinical | admin | both | conditional.</summary>
    /// <example>clinical</example>
    public string? DocumentationNeeded { get; set; }

    /// <summary>Why the payer wants documentation: withpa | withclaim | withorder | retain-doc.</summary>
    /// <example>["withpa"]</example>
    public List<string> DocumentationPurpose { get; set; } = new();

    /// <summary>Extra information the payer needs before it can decide: performer | location | timeframe | contract-window.</summary>
    /// <example>[]</example>
    public List<string> InformationNeeded { get; set; } = new();

    /// <summary>Payer questionnaires to fill in (DTR).</summary>
    /// <example>["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"]</example>
    public List<string> QuestionnaireUrls { get; set; } = new();

    /// <summary>The payer's id for this answer.</summary>
    /// <example>702644806bf74c70aa65d2ca1a4a34b1</example>
    public string? CoverageAssertionId { get; set; }

    /// <summary>Authorization number, when an authorization already exists (priorAuthNeeded = satisfied).</summary>
    /// <example>PA20260706451203</example>
    public string? SatisfiedAuthorizationNumber { get; set; }

    /// <summary>Messages of the payer for the user.</summary>
    public List<EhrMessageDto> Messages { get; set; } = new();

    /// <summary>Date of the decision.</summary>
    /// <example>2026-10-07</example>
    public string? DecidedAt { get; set; }
}
