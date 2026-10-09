using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR DeviceRequest (CRD profile): an order for durable medical equipment (EHR orderType "device").
/// </summary>
public class DeviceRequest : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "DeviceRequest";

    public string Status { get; set; } = "draft";
    public string Intent { get; set; } = "order";
    public string? Priority { get; set; }
    /// <summary>The device (HCPCS code).</summary>
    public CodeableConcept? CodeCodeableConcept { get; set; }
    public ResourceReference? Subject { get; set; }
    public ResourceReference? Encounter { get; set; }
    public string? OccurrenceDateTime { get; set; }
    public Period? OccurrencePeriod { get; set; }
    public string? AuthoredOn { get; set; }
    public ResourceReference? Requester { get; set; }
    public ResourceReference? Performer { get; set; }
    public List<CodeableConcept> ReasonCode { get; set; } = new();
    public List<ResourceReference> Insurance { get; set; } = new();
    public List<Annotation> Note { get; set; } = new();
}
