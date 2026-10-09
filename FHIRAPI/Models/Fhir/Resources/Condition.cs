using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Condition (US Core): a diagnosis or problem.
/// </summary>
public class Condition : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Condition";

    public CodeableConcept? ClinicalStatus { get; set; }
    public CodeableConcept? VerificationStatus { get; set; }
    public List<CodeableConcept> Category { get; set; } = new();
    /// <summary>The diagnosis (ICD-10-CM).</summary>
    public CodeableConcept Code { get; set; } = new();
    public ResourceReference? Subject { get; set; }
    public ResourceReference? Encounter { get; set; }
    public string? OnsetDateTime { get; set; }
    public string? RecordedDate { get; set; }
}
