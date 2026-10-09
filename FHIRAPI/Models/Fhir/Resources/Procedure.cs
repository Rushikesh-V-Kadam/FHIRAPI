using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Procedure (US Core): a procedure already performed.
/// </summary>
public class Procedure : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Procedure";

    public string Status { get; set; } = "completed";
    public CodeableConcept Code { get; set; } = new();
    public ResourceReference? Subject { get; set; }
    public ResourceReference? Encounter { get; set; }
    public string? PerformedDateTime { get; set; }
    public List<ProcedurePerformer> Performer { get; set; } = new();
}
