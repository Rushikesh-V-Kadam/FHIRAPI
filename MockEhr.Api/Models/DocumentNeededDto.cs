namespace MockEhr.Api.Models;

/// <summary>
/// One document the payer asks for.
/// </summary>
public class DocumentNeededDto
{
    /// <summary>LOINC document type the payer wants, e.g. 11502-2 Laboratory report.</summary>
    /// <example>11502-2</example>
    public string LoincCode { get; set; } = string.Empty;

    /// <summary>Text of the type.</summary>
    /// <example>Laboratory report</example>
    public string? Display { get; set; }

    /// <summary>Line of the request the document is for. Empty = the whole request.</summary>
    /// <example>1</example>
    public int? LineNumber { get; set; }
}
