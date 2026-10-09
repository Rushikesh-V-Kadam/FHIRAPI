using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Observation (US Core lab result or vital sign). Exactly one value[x] (or DataAbsentReason) is set.
/// </summary>
public class Observation : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Observation";

    public string Status { get; set; } = "final";
    public List<CodeableConcept> Category { get; set; } = new();
    /// <summary>What was measured (LOINC).</summary>
    public CodeableConcept Code { get; set; } = new();
    public ResourceReference? Subject { get; set; }
    public ResourceReference? Encounter { get; set; }
    public string? EffectiveDateTime { get; set; }
    public string? Issued { get; set; }
    public Quantity? ValueQuantity { get; set; }
    public string? ValueString { get; set; }
    public CodeableConcept? ValueCodeableConcept { get; set; }
    public CodeableConcept? DataAbsentReason { get; set; }
}
