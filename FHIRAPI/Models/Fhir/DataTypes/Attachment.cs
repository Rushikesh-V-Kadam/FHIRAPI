namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Attachment: a file, either embedded (Data, base64) or linked (Url).
/// </summary>
public class Attachment
{
    public string? ContentType { get; set; }
    /// <summary>File content, base64 encoded.</summary>
    public string? Data { get; set; }
    /// <summary>Where the file can be downloaded (our Binary endpoint).</summary>
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Creation { get; set; }
}
