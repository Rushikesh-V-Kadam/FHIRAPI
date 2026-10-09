namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Document metadata from GET api/documents/{documentId} or api/patients/{patientId}/documents.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrDocumentDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string TypeCode { get; set; } = string.Empty;
    public string TypeCodeSystem { get; set; } = "LOINC";
    public string? TypeDisplay { get; set; }
    public string? Category { get; set; }
    public string? Title { get; set; }
    public string? CreatedDateTime { get; set; }
    public string? AuthorPractitionerId { get; set; }
    public string? OrganizationId { get; set; }
    public string? EncounterId { get; set; }
    public string Status { get; set; } = "current";
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public string? ContentUrl { get; set; }
}
