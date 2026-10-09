namespace MockEhr.Api.Models;

/// <summary>
/// The file of a clinical document, as the EHR returns it from GET api/documents/{documentId}/content.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Binary; the gateway embeds it in the request to the payer.
/// </summary>
public class EhrDocumentContentDto
{
    /// <summary>EHR document id.</summary>
    /// <example>doc-1-labs</example>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>MIME type of the file. Empty = application/octet-stream.</summary>
    /// <example>text/plain</example>
    public string? ContentType { get; set; }

    /// <summary>The whole file, Base64 encoded.</summary>
    /// <example>TW9udGhseSBkaWFseXNpcyBsYWJzIC0gU2VwdGVtYmVyIDIwMjY=</example>
    public string ContentBase64 { get; set; } = string.Empty;
}
