using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Location (US Core). Type holds the CMS place-of-service code.
/// </summary>
public class Location : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Location";

    public string? Status { get; set; }
    public string? Name { get; set; }
    public List<CodeableConcept> Type { get; set; } = new();
    public Address? Address { get; set; }
    public ResourceReference? ManagingOrganization { get; set; }
}
