using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Extension: extra data that is not part of the base resource (e.g. DTR qr-context, US Core race).
/// Only one value[x] property is set at a time. A "complex" extension has nested extensions instead of a value.
/// </summary>
public class Extension
{
    /// <summary>Identifies the meaning of the extension.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Nested extensions. Named Children because a C# member cannot share the class name; written as "extension".</summary>
    [JsonPropertyName("extension")]
    public List<Extension> Children { get; set; } = new();

    public string? ValueString { get; set; }
    public string? ValueCode { get; set; }
    public bool? ValueBoolean { get; set; }
    public Coding? ValueCoding { get; set; }
    public ResourceReference? ValueReference { get; set; }
}
