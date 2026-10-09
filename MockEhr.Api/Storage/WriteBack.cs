using System.Text.Json;

namespace MockEhr.Api.Storage;

/// <summary>One thing the gateway or the FHIR API saved in the EHR (shown by GET /api/write-backs).</summary>
public class WriteBack
{
    /// <summary>When it was saved (UTC).</summary>
    /// <example>2026-10-07T18:36:17.2908898+00:00</example>
    public DateTimeOffset At { get; set; }

    /// <summary>coverage-decision | questionnaire-response | prior-auth-decision</summary>
    /// <example>coverage-decision</example>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Order id, form id or request id, depending on kind.</summary>
    /// <example>ord-1-hd</example>
    public string Id { get; set; } = string.Empty;

    /// <summary>What was saved (JSON).</summary>
    public JsonElement Body { get; set; }
}
