namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Insurance (coverage) from GET api/patients/{patientId}/insurances.
/// Plain EHR JSON (camelCase) - not FHIR.
/// The EHR has no id for an insurance: the FHIR Coverage id is built from patientId + payerId + memberId
/// (the policy number), see Helpers/CoverageIds.
/// </summary>
public class EhrInsuranceDto
{
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    /// <summary>Policy number / member id on the insurance card. Required: part of the Coverage id.</summary>
    public string MemberId { get; set; } = string.Empty;
    public string PayerId { get; set; } = string.Empty;
    public string? PayerName { get; set; }
    public string? PlanId { get; set; }
    public string? PlanName { get; set; }
    public string? GroupNumber { get; set; }
    public string? RelationshipToSubscriber { get; set; }
    public EhrSubscriberDto? Subscriber { get; set; }
    public string? EffectiveDate { get; set; }
    public string? TerminationDate { get; set; }
    public int? CoverageOrder { get; set; }
    public string? CoverageType { get; set; }
}
