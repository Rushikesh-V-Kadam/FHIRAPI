namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Order from GET api/orders/{orderId}. orderType decides the FHIR resource: service, device or medication.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrOrderDto
{
    public string OrderId { get; set; } = string.Empty;
    /// <summary>service | device | medication</summary>
    public string OrderType { get; set; } = "service";
    public string PatientId { get; set; } = string.Empty;
    public string? EncounterId { get; set; }
    public string Status { get; set; } = "draft";
    public string Code { get; set; } = string.Empty;
    public string CodeSystem { get; set; } = string.Empty;
    public string? CodeDisplay { get; set; }
    public decimal? Quantity { get; set; }
    public string? OrderedDateTime { get; set; }
    public string? RequestedStartDate { get; set; }
    public string? RequestedEndDate { get; set; }
    public string? OrderingPractitionerId { get; set; }
    public string? PerformerOrganizationId { get; set; }
    public string? PerformerPractitionerId { get; set; }
    public string? PlaceOfServiceCode { get; set; }
    public string? LocationId { get; set; }
    public List<EhrCode> Diagnoses { get; set; } = new();
    public string? CoverageId { get; set; }
    public string? Priority { get; set; }
    public string? Notes { get; set; }
    public EhrMedicationDetailsDto? Medication { get; set; }
}
