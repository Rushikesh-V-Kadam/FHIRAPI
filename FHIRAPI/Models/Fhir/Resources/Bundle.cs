using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Bundle. This API returns "searchset" Bundles from every search.
/// </summary>
public class Bundle : Resource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Bundle";

    /// <summary>Always "searchset" here.</summary>
    public string Type { get; set; } = "searchset";

    public string? Timestamp { get; set; }

    /// <summary>Number of matches (not counting _include entries).</summary>
    public int? Total { get; set; }

    public List<BundleLink> Link { get; set; } = new();
    public List<BundleEntry> Entry { get; set; } = new();
}
