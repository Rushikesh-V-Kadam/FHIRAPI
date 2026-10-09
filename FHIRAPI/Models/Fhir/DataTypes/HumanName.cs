namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR HumanName: a person's name.
/// </summary>
public class HumanName
{
    /// <summary>usual | official ...</summary>
    public string? Use { get; set; }
    /// <summary>Last name.</summary>
    public string? Family { get; set; }
    /// <summary>First and middle names.</summary>
    public List<string> Given { get; set; } = new();
}
