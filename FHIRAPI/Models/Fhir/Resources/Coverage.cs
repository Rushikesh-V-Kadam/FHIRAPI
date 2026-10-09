using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Coverage (Da Vinci CRD Coverage profile): the patient's insurance.
/// The payer is identified by Payor[0].Identifier (payer id), the plan by Class (type "plan").
/// </summary>
public class Coverage : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Coverage";

    public List<Identifier> Identifier { get; set; } = new();
    public string Status { get; set; } = "active";
    public CodeableConcept? Type { get; set; }
    /// <summary>Member id.</summary>
    public string? SubscriberId { get; set; }
    /// <summary>The patient this coverage is for.</summary>
    public ResourceReference? Beneficiary { get; set; }
    public CodeableConcept? Relationship { get; set; }
    public Period? Period { get; set; }
    public List<ResourceReference> Payor { get; set; } = new();
    public List<CoverageClass> Class { get; set; } = new();
}
