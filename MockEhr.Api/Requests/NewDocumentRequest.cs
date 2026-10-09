using System.ComponentModel;

namespace MockEhr.Api.Requests;

/// <summary>
/// Body of POST /api/patients/{id}/documents: a clinical document added in the EHR app.
/// Send the file as contentBase64 (uploaded file) or contentText (typed note).
/// </summary>
public class NewDocumentRequest
{
    /// <summary>Document id. Empty = a new id is created.</summary>
    /// <example></example>
    public string? DocumentId { get; set; }

    /// <summary>LOINC document type, e.g. 11506-3 Progress note, 18748-4 Diagnostic imaging study, 18842-5 Discharge summary. Must be filled.</summary>
    /// <example>11506-3</example>
    public string TypeCode { get; set; } = string.Empty;

    /// <summary>Text of the type.</summary>
    /// <example>Progress note</example>
    public string? TypeDisplay { get; set; }

    /// <summary>Title shown to the user. Empty = typeDisplay.</summary>
    /// <example>Nephrology progress note</example>
    public string? Title { get; set; }

    /// <summary>Clinician who wrote the document.</summary>
    /// <example>prac-1</example>
    public string? AuthorPractitionerId { get; set; }

    /// <summary>Organization that keeps the document.</summary>
    /// <example>org-1</example>
    public string? OrganizationId { get; set; }

    /// <summary>Encounter the document belongs to.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>MIME type of the content: text/plain, application/pdf, image/png ...</summary>
    /// <example>text/plain</example>
    [DefaultValue("text/plain")]
    public string ContentType { get; set; } = "text/plain";

    /// <summary>Typed text (used when contentBase64 is empty).</summary>
    /// <example>Patient on hemodialysis 3x/week.</example>
    public string? ContentText { get; set; }

    /// <summary>File content as Base64 (uploaded file). Wins over contentText.</summary>
    /// <example></example>
    public string? ContentBase64 { get; set; }
}
