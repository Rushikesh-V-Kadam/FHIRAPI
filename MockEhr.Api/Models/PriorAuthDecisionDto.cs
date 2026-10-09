using System.Text.Json.Serialization;

namespace MockEhr.Api.Models;

/// <summary>
/// Prior authorization decision, as the mock EHR prior authorization screen shows it (GET api/prior-auth-requests).
/// It is read from the decisionJson column of table PA_PRIOR_AUTH: the same structure as the response of the
/// gateway's POST /api/v1/pas/requests.
/// </summary>
public class PriorAuthDecisionDto
{
    /// <summary>The EHR's request id.</summary>
    /// <example>PA-2026-0001</example>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>The payer's id.</summary>
    /// <example>PAYER001</example>
    public string? PayerId { get; set; }

    /// <summary>Payer name sent by the Payer Gateway.</summary>
    /// <example>MockPayer</example>
    public string? PayerName { get; set; }

    /// <summary>EHR patient id.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Orders in the request.</summary>
    /// <example>["ord-1-hd"]</example>
    public List<string> OrderIds { get; set; } = new();

    /// <summary>approved | partially-approved | denied | pended | not-required | cancelled | contact-payer | error.</summary>
    /// <example>approved</example>
    public string OverallStatus { get; set; } = "pended";

    /// <summary>Authorization number; filled when the payer approved.</summary>
    /// <example>PA20261007123456</example>
    public string? AuthorizationNumber { get; set; }

    /// <summary>First day the authorization is valid, yyyy-MM-dd.</summary>
    /// <example>2026-10-05</example>
    public string? ValidFrom { get; set; }

    /// <summary>Last day the authorization is valid, yyyy-MM-dd.</summary>
    /// <example>2027-04-03</example>
    public string? ValidTo { get; set; }

    /// <summary>When the payer decided (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-10-07T18:36:17Z</example>
    public string? DecidedAt { get; set; }

    /// <summary>Pended requests: when the payer must have decided (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-10-14T18:36:17Z</example>
    public string? DecisionDueBy { get; set; }

    /// <summary>The decision per order line.</summary>
    public List<PriorAuthLineDto> Lines { get; set; } = new();

    /// <summary>What the payer still needs (pended requests).</summary>
    public List<InformationRequestDto> InformationRequests { get; set; } = new();

    /// <summary>Problems the payer reported.</summary>
    /// <example>[]</example>
    public List<string> Errors { get; set; } = new();

    /// <summary>Old write-back only: the payer's FHIR response.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PayerResponseFhirJson { get; set; }
}
