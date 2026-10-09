namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Insurance (coverage) from GET api/patients/{patientId}/insurances.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrInsuranceDto
{
    public string CoverageId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
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
