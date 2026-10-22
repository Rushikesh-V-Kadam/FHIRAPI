namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Insurance (coverage) from GET api/insurances/{coverageId} (one) or GET api/patients/{patientId}/insurances (list).
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrInsuranceDto
{
    /// <summary>
    /// The EHR's id of this insurance, unique in the EHR. It is the FHIR Coverage.id and the coverageId the EHR sends
    /// to the Payer Gateway (order-sign, DTR, PAS).
    /// </summary>
    // CLIENT-EHR: the client sends its own unique coverage id.
    public string CoverageId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    /// <summary>Policy number / member id on the insurance card (Coverage.subscriberId, optional).</summary>
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
