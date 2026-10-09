namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Document file from GET api/documents/{documentId}/content (base64).
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrDocumentContentDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string ContentBase64 { get; set; } = string.Empty;
}
