namespace MockEhr.Api.Models;

/// <summary>
/// A message of the payer for the user (from a CRD card).
/// </summary>
public class EhrMessageDto
{
    /// <summary>info | warning | critical.</summary>
    /// <example>info</example>
    public string Severity { get; set; } = "info";

    /// <summary>Short text.</summary>
    /// <example>Prior authorization required: In-center hemodialysis, 3 times a week</example>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Longer text.</summary>
    /// <example>Complete the documentation (DTR), then submit the request (PAS).</example>
    public string? Detail { get; set; }

    /// <summary>A link that belongs to the message, if any.</summary>
    public EhrMessageLinkDto? Link { get; set; }
}
