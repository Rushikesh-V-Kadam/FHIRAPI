using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR ServiceRequest (CRD profile): an order for a procedure, imaging, therapy... (EHR orderType "service").
/// </summary>
public class ServiceRequest : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "ServiceRequest";

    public string Status { get; set; } = "draft";
    public string Intent { get; set; } = "order";
    public string? Priority { get; set; }
    /// <summary>What is ordered (CPT / HCPCS code).</summary>
    public CodeableConcept? Code { get; set; }
    public Quantity? QuantityQuantity { get; set; }
    public ResourceReference? Subject { get; set; }
    public ResourceReference? Encounter { get; set; }
    public string? OccurrenceDateTime { get; set; }
    public Period? OccurrencePeriod { get; set; }
    public string? AuthoredOn { get; set; }
    public ResourceReference? Requester { get; set; }
    public List<ResourceReference> Performer { get; set; } = new();
    public List<CodeableConcept> LocationCode { get; set; } = new();
    public List<ResourceReference> LocationReference { get; set; } = new();
    /// <summary>Diagnoses (ICD-10-CM).</summary>
    public List<CodeableConcept> ReasonCode { get; set; } = new();
    public List<ResourceReference> Insurance { get; set; } = new();
    public List<Annotation> Note { get; set; } = new();
}
