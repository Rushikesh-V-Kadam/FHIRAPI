using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR MedicationRequest (CRD profile): a prescription (EHR orderType "medication").
/// </summary>
public class MedicationRequest : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "MedicationRequest";

    public string Status { get; set; } = "draft";
    public string Intent { get; set; } = "order";
    public string? Priority { get; set; }
    /// <summary>The drug (RxNorm code).</summary>
    public CodeableConcept? MedicationCodeableConcept { get; set; }
    public ResourceReference? Subject { get; set; }
    public ResourceReference? Encounter { get; set; }
    public string? AuthoredOn { get; set; }
    public ResourceReference? Requester { get; set; }
    public List<CodeableConcept> ReasonCode { get; set; } = new();
    public List<ResourceReference> Insurance { get; set; } = new();
    public List<Dosage> DosageInstruction { get; set; } = new();
    public MedicationDispenseRequest? DispenseRequest { get; set; }
    public List<Annotation> Note { get; set; } = new();
}
