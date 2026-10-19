using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Requests;

/// <summary>
/// Form of POST /api/patients/{id}/documents/upload: a clinical document added as a file (PDF, image, text, Word).
/// The file is sent as it is (multipart/form-data); the EHR converts it to Base64 itself, so the caller does not
/// have to. Use POST /api/patients/{id}/documents (JSON) when the content is already Base64 or typed text.
/// </summary>
public class UploadDocumentRequest
{
    /// <summary>The file. Must be filled. Allowed: .pdf, .png, .jpg, .jpeg, .tif, .tiff, .txt, .rtf, .doc, .docx (at most 20 MB).</summary>
    [Required]
    public IFormFile? File { get; set; }

    /// <summary>LOINC document type, e.g. 11502-2 Laboratory report, 11506-3 Progress note, 18748-4 Diagnostic imaging study. Must be filled.</summary>
    /// <example>11502-2</example>
    [Required]
    public string TypeCode { get; set; } = string.Empty;

    /// <summary>Text of the type.</summary>
    /// <example>Laboratory report</example>
    public string? TypeDisplay { get; set; }

    /// <summary>Title shown to the user. Empty = the file name without its extension.</summary>
    /// <example>Monthly dialysis labs - October 2026</example>
    public string? Title { get; set; }

    /// <summary>Document id. Empty = a new id is created (doc-xxxxxxxx). Must not be used by another document.</summary>
    /// <example></example>
    public string? DocumentId { get; set; }

    /// <summary>Clinician who wrote the document.</summary>
    /// <example>prac-1</example>
    public string? AuthorPractitionerId { get; set; }

    /// <summary>Organization that keeps the document.</summary>
    /// <example>org-1</example>
    public string? OrganizationId { get; set; }

    /// <summary>Encounter the document belongs to.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }
}
