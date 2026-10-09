using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One order-sign event and the answer the gateway returned for it = one row of table PA_CRD_EVENT.
/// Body of PUT api/prior-auth-data/crd-events/{eventId}; returned by GET of the same path.
/// A repeated eventId gets the stored answer (no second payer call).
/// </summary>
public class CrdEventRow
{
    /// <summary>Key. The EHR's id of the signing event (the same value as in the URL). Column EVENT_ID, VARCHAR2(100).</summary>
    /// <example>evt-20261007-0001</example>
    public string EventId { get; set; } = string.Empty;

    /// <summary>EHR patient id. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Payer that was asked. Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>
    /// JSON text: the whole answer the gateway returned for the event (the response of POST /api/v1/crd/order-sign).
    /// Store the text unchanged and return it unchanged. Column RESPONSE_JSON, CLOB.
    /// </summary>
    /// <example>{"eventId":"evt-20261007-0001","payerName":"MockPayer","decisions":[{"orderId":"ord-1-hd","covered":"covered","callDtr":true,"callPas":true}]}</example>
    public string ResponseJson { get; set; } = string.Empty;

    /// <summary>When the event was answered (UTC, ISO-8601 with Z). Column CREATED_AT, TIMESTAMP.</summary>
    /// <example>2026-10-07T18:35:40.1234567Z</example>
    [Required]
    public DateTime CreatedAt { get; set; }
}
