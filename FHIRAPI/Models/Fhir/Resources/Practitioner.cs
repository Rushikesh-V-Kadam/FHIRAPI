using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Practitioner (US Core). The NPI is in Identifier.
/// </summary>
public class Practitioner : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Practitioner";

    public List<Identifier> Identifier { get; set; } = new();
    public List<HumanName> Name { get; set; } = new();
}
