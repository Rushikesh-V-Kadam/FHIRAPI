using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Organization (US Core). NPI and tax id are in Identifier.
/// </summary>
public class Organization : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Organization";

    public List<Identifier> Identifier { get; set; } = new();
    public bool? Active { get; set; }
    public string? Name { get; set; }
    public List<ContactPoint> Telecom { get; set; } = new();
    public List<Address> Address { get; set; } = new();
}
