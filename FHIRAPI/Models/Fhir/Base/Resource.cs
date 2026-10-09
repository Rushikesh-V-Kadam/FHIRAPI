using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Base class of every FHIR resource. ResourceType is written first in the JSON.
/// </summary>
public abstract class Resource
{
    /// <summary>FHIR resource type name, e.g. "Patient". Set by each subclass.</summary>
    [JsonPropertyOrder(-100)]
    public abstract string ResourceType { get; }

    /// <summary>Logical id of the resource on this server.</summary>
    [JsonPropertyOrder(-99)]
    public string? Id { get; set; }

    [JsonPropertyOrder(-98)]
    public Meta? Meta { get; set; }

    /// <summary>Adds a profile URL to meta.profile.</summary>
    public void AddProfile(string profileUrl)
    {
        Meta ??= new Meta();
        if (!Meta.Profile.Contains(profileUrl)) Meta.Profile.Add(profileUrl);
    }
}
