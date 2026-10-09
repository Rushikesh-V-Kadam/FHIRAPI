namespace MockEhr.Api.Models;

/// <summary>
/// A clinical document (the description, not the file), as the EHR returns it from GET api/documents/{documentId}
/// and GET api/patients/{patientId}/documents. Plain JSON (camelCase), not FHIR.
/// The FHIR API turns it into a FHIR DocumentReference. Documents are attached to prior authorization requests (PAS)
/// and sent when the payer asks for more information (CDex); the payer asks for them by LOINC type code.
/// </summary>
public class EhrDocumentDto
{
    /// <summary>EHR document id. Used in documentIds of gateway requests.</summary>
    /// <example>doc-1-labs</example>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Patient the document is about.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>LOINC document type, e.g. 11502-2 Laboratory report, 11506-3 Progress note, 18842-5 Discharge summary. The typeCode filter of the list compares with this value.</summary>
    /// <example>11502-2</example>
    public string TypeCode { get; set; } = string.Empty;

    /// <summary>Code system of typeCode.</summary>
    /// <example>LOINC</example>
    public string TypeCodeSystem { get; set; } = "LOINC";

    /// <summary>Text of the type.</summary>
    /// <example>Laboratory report</example>
    public string? TypeDisplay { get; set; }

    /// <summary>Document category. Empty = clinical-note.</summary>
    /// <example>clinical-note</example>
    public string? Category { get; set; }

    /// <summary>Title shown to the user.</summary>
    /// <example>Monthly dialysis labs - September 2026</example>
    public string? Title { get; set; }

    /// <summary>When the document was created (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-09-22T12:00:00Z</example>
    public string? CreatedDateTime { get; set; }

    /// <summary>Clinician who wrote it.</summary>
    /// <example>prac-1</example>
    public string? AuthorPractitionerId { get; set; }

    /// <summary>Organization that keeps the document.</summary>
    /// <example>org-1</example>
    public string? OrganizationId { get; set; }

    /// <summary>Encounter the document belongs to.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>current | superseded | entered-in-error.</summary>
    /// <example>current</example>
    public string Status { get; set; } = "current";

    /// <summary>MIME type of the file: text/plain, application/pdf, image/png ...</summary>
    /// <example>application/pdf</example>
    public string? ContentType { get; set; }

    /// <summary>Size of the file in bytes.</summary>
    /// <example>48213</example>
    public long? SizeBytes { get; set; }

    /// <summary>Where the file can be read (information only; the FHIR API always calls GET api/documents/{documentId}/content).</summary>
    /// <example>/api/documents/doc-1-labs/content</example>
    public string? ContentUrl { get; set; }
}
