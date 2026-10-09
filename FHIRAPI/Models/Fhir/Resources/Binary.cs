using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Binary: the raw content of an EHR document (PDF, image...), base64 encoded.
/// DocumentReference.content.attachment.url points here.
/// </summary>
public class Binary : Resource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Binary";

    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>File content, base64 encoded.</summary>
    public string? Data { get; set; }
}
