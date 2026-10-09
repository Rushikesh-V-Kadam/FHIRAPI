namespace MockEhr.Api.Models;

/// <summary>
/// A link on a payer message.
/// </summary>
public class EhrMessageLinkDto
{
    /// <summary>Text of the link.</summary>
    /// <example>Coverage policy</example>
    public string Label { get; set; } = string.Empty;

    /// <summary>Where the link goes.</summary>
    /// <example>https://example.org/policies/outpatient-dialysis</example>
    public string Url { get; set; } = string.Empty;

    /// <summary>absolute | smart (smart links are rewritten to the gateway DTR launch URL).</summary>
    /// <example>absolute</example>
    public string Type { get; set; } = "absolute";
}
