namespace MockEhr.Api.Models;

/// <summary>
/// Something the payer still needs for a pended request (a CDex Task): documents and / or questionnaires.
/// </summary>
public class InformationRequestDto
{
    /// <summary>Tracking id of the payer's request; sent back with the documents.</summary>
    /// <example>TRK-acdb297bc3a043dd</example>
    public string TrackingId { get; set; } = string.Empty;

    /// <summary>The payer's Task id.</summary>
    /// <example>5f367f122eba45caba287738b1cc2a38</example>
    public string? TaskId { get; set; }

    /// <summary>Date the payer wants the documents by.</summary>
    /// <example>2026-10-21</example>
    public string? DueDate { get; set; }

    /// <summary>requested | in-progress | completed | cancelled.</summary>
    /// <example>requested</example>
    public string Status { get; set; } = "requested";

    /// <summary>Documents the payer asks for, by LOINC type.</summary>
    public List<DocumentNeededDto> DocumentsNeeded { get; set; } = new();

    /// <summary>Questionnaires the payer asks to be filled in (URLs).</summary>
    /// <example>[]</example>
    public List<string> QuestionnairesNeeded { get; set; } = new();
}
