namespace MockEhr.Api.Models;

/// <summary>
/// One insurance (coverage) of a patient, as the EHR returns it from GET api/patients/{patientId}/insurances.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Coverage; the payer finds the member with memberId.
/// </summary>
public class EhrInsuranceDto
{
    /// <summary>EHR id of this insurance. Used as coverageId in gateway requests.</summary>
    /// <example>cov-1</example>
    public string CoverageId { get; set; } = string.Empty;

    /// <summary>Patient who is insured.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>active | cancelled ... The gateway asks for status=active.</summary>
    /// <example>active</example>
    public string Status { get; set; } = "active";

    /// <summary>Member id on the insurance card. Sent to the payer in every request.</summary>
    /// <example>MBR-1001</example>
    public string MemberId { get; set; } = string.Empty;

    /// <summary>The payer's id as the EHR knows it (becomes Coverage.payor.identifier).</summary>
    /// <example>PAYER001</example>
    public string PayerId { get; set; } = string.Empty;

    /// <summary>Name of the insurance company (display text).</summary>
    /// <example>Mock Da Vinci Payer</example>
    public string? PayerName { get; set; }

    /// <summary>Plan id (becomes Coverage.class "plan").</summary>
    /// <example>GOLD-PPO</example>
    public string? PlanId { get; set; }

    /// <summary>Plan name.</summary>
    /// <example>Gold PPO</example>
    public string? PlanName { get; set; }

    /// <summary>Group number (becomes Coverage.class "group").</summary>
    /// <example>GRP-500</example>
    public string? GroupNumber { get; set; }

    /// <summary>self | spouse | child | parent | other. Empty = self.</summary>
    /// <example>self</example>
    public string? RelationshipToSubscriber { get; set; }

    /// <summary>The subscriber, when the patient is not the subscriber (relationship is not self).</summary>
    public EhrSubscriberDto? Subscriber { get; set; }

    /// <summary>First day of coverage, yyyy-MM-dd.</summary>
    /// <example>2026-01-01</example>
    public string? EffectiveDate { get; set; }

    /// <summary>Last day of coverage, yyyy-MM-dd.</summary>
    /// <example>2027-12-31</example>
    public string? TerminationDate { get; set; }

    /// <summary>1 = primary, 2 = secondary ... The first active coverage is used when a request names none.</summary>
    /// <example>1</example>
    public int? CoverageOrder { get; set; }

    /// <summary>Type of coverage as a v3 ActCode, e.g. HIP (health insurance plan), MCPOL (managed care).</summary>
    /// <example>HIP</example>
    public string? CoverageType { get; set; }
}
