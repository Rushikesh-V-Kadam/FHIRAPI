using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One prior authorization request and its latest decision = one row of table PA_PRIOR_AUTH.
/// Body of PUT api/prior-auth-data/prior-auths/{requestId}; returned by GET of the same path and by the list.
/// The row is saved again on every update, cancel, inquiry and payer answer. The EHR prior authorization screen reads this table.
/// Lists, the decision and the FHIR Bundles are sent as JSON text so they can be stored unchanged in CLOB columns.
/// </summary>
public class PriorAuthRow
{
    /// <summary>Key. The EHR's request id (the same value as in the URL). Column REQUEST_ID, VARCHAR2(64).</summary>
    /// <example>PA-2026-0001</example>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Payer the request was sent to. Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>EHR patient id. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Insurance used for the request. Column COVERAGE_ID, VARCHAR2(64).</summary>
    /// <example>cov-1</example>
    public string CoverageId { get; set; } = string.Empty;

    /// <summary>Member id on that insurance. Column MEMBER_ID, VARCHAR2(64).</summary>
    /// <example>MBR-1001</example>
    public string? MemberId { get; set; }

    /// <summary>Provider organization that asked for the authorization. Column ORGANIZATION_ID, VARCHAR2(64).</summary>
    /// <example>org-1</example>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>NPI of that organization. Column ORGANIZATION_NPI, VARCHAR2(20).</summary>
    /// <example>1999999992</example>
    public string? OrganizationNpi { get; set; }

    /// <summary>Requesting clinician. Column PRACTITIONER_ID, VARCHAR2(64).</summary>
    /// <example>prac-1</example>
    public string? PractitionerId { get; set; }

    /// <summary>NPI of that clinician. Column PRACTITIONER_NPI, VARCHAR2(20).</summary>
    /// <example>1111111112</example>
    public string? PractitionerNpi { get; set; }

    /// <summary>routine | urgent. Column PRIORITY, VARCHAR2(10).</summary>
    /// <example>routine</example>
    public string Priority { get; set; } = "routine";

    /// <summary>
    /// Overall status: approved | partially-approved | denied | pended | not-required | cancelled | contact-payer | error.
    /// The gateway asks for the rows with status "pended" (GET prior-auths?status=pended). Column STATUS, VARCHAR2(20).
    /// </summary>
    /// <example>pended</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>Authorization number; filled when the payer approved. Column AUTHORIZATION_NUMBER, VARCHAR2(64).</summary>
    /// <example>PA20261007123456</example>
    public string? AuthorizationNumber { get; set; }

    /// <summary>1 = first submission, +1 for every update / cancel. Column VERSION_NO, NUMBER(5).</summary>
    /// <example>1</example>
    [Required]
    public int VersionNo { get; set; }

    /// <summary>JSON text: array of the order ids in the request. Column ORDER_IDS_JSON, CLOB.</summary>
    /// <example>["ord-1-hd"]</example>
    public string? OrderIdsJson { get; set; }

    /// <summary>JSON text: object orderId -> claim line number. Column LINE_NUMBERS_JSON, CLOB.</summary>
    /// <example>{"ord-1-hd":1}</example>
    public string? LineNumbersJson { get; set; }

    /// <summary>
    /// JSON text: the full decision, the same structure as the response of POST /api/v1/pas/requests
    /// (overallStatus, lines, informationRequests, decisionDueBy ...). The EHR screen reads the line decisions from it.
    /// Store the text unchanged and return it unchanged. Column DECISION_JSON, CLOB.
    /// </summary>
    /// <example>{"requestId":"PA-2026-0001","payerName":"MockPayer","patientId":"pat-1","orderIds":["ord-1-hd"],"overallStatus":"pended","lines":[{"sequence":1,"orderId":"ord-1-hd","decision":"pended"}],"informationRequests":[],"errors":[]}</example>
    public string? DecisionJson { get; set; }

    /// <summary>
    /// JSON text: the last FHIR Bundle sent to the payer. Can be several MB (the clinical documents are inside),
    /// so allow large request bodies. Store unchanged and return unchanged. Column LAST_REQUEST_BUNDLE, CLOB.
    /// </summary>
    /// <example>{"resourceType":"Bundle","type":"collection","entry":[]}</example>
    public string? LastRequestBundle { get; set; }

    /// <summary>JSON text: the last FHIR Bundle received from the payer. Store unchanged and return unchanged. Column LAST_RESPONSE_BUNDLE, CLOB.</summary>
    /// <example>{"resourceType":"Bundle","type":"collection","entry":[]}</example>
    public string? LastResponseBundle { get; set; }

    /// <summary>When the request was first sent (UTC, ISO-8601 with Z). Column SUBMITTED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:36:17.0159473Z</example>
    [Required]
    public DateTime SubmittedAt { get; set; }

    /// <summary>When the row was last saved; changes on every save (UTC, ISO-8601 with Z). Column UPDATED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:36:17.2908898Z</example>
    [Required]
    public DateTime UpdatedAt { get; set; }
}
