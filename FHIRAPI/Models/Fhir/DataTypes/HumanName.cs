namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR HumanName: a person's name.
/// </summary>
public class HumanName
{
    /// <summary>usual | official ...</summary>
    // R4: HumanName.use 0..1 - optional
    public string? Use { get; set; }
    /// <summary>The full name as one text, when the source does not split it into parts.</summary>
    // R4: HumanName.text 0..1 - optional
    public string? Text { get; set; }
    /// <summary>Last name.</summary>
    public string? Family { get; set; }
    /// <summary>First and middle names.</summary>
    public List<string> Given { get; set; } = new();
}
