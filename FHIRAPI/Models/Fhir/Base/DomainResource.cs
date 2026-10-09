using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Base class of clinical and administrative resources: adds extensions.
/// </summary>
public abstract class DomainResource : Resource
{
    [JsonPropertyOrder(-95)]
    public List<Extension> Extension { get; set; } = new();
}
