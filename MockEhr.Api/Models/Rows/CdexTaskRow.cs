using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One payer information request (a FHIR Task: "send these documents") = one row of table PA_CDEX_TASK.
/// Body of PUT api/prior-auth-data/cdex-tasks/{payerName}/{taskId}; returned by GET cdex-tasks?requestId=.
/// </summary>
public class CdexTaskRow
{
    /// <summary>Key, part 1 (the same value as in the URL; upper / lower case does not matter). Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>Key, part 2: the payer's Task id (the same value as in the URL). Column TASK_ID, VARCHAR2(100).</summary>
    /// <example>5f367f122eba45caba287738b1cc2a38</example>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>The prior authorization request the Task belongs to (PA_PRIOR_AUTH.REQUEST_ID). Column REQUEST_ID, VARCHAR2(64).</summary>
    /// <example>PA-2026-0001</example>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>Tracking id; sent back to the payer with the documents. Column TRACKING_ID, VARCHAR2(100).</summary>
    /// <example>TRK-acdb297bc3a043dd</example>
    public string TrackingId { get; set; } = string.Empty;

    /// <summary>Identifier system of the tracking id; sent back unchanged. Column TRACKING_SYSTEM, VARCHAR2(500).</summary>
    /// <example>http://example.org/cdex/tracking-ids</example>
    public string? TrackingSystem { get; set; }

    /// <summary>Task status as the payer sent it: requested | in-progress | completed | cancelled ... Column STATUS, VARCHAR2(20).</summary>
    /// <example>requested</example>
    public string? Status { get; set; }

    /// <summary>Date text as the payer sent it (not converted). Column DUE_DATE, VARCHAR2(30).</summary>
    /// <example>2026-10-21</example>
    public string? DueDate { get; set; }

    /// <summary>JSON text: the FHIR Task as received. Store unchanged and return unchanged. Column TASK_JSON, CLOB.</summary>
    /// <example>{"resourceType":"Task","id":"5f367f122eba45caba287738b1cc2a38","status":"requested","intent":"order"}</example>
    public string TaskJson { get; set; } = string.Empty;

    /// <summary>When the gateway received the Task (UTC, ISO-8601 with Z). Column RECEIVED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:40:02.1000000Z</example>
    [Required]
    public DateTime ReceivedAt { get; set; }
}
