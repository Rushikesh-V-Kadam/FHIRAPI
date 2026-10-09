namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Meta: resource metadata (profiles it claims to follow, last update time).
/// </summary>
public class Meta
{
    public string? LastUpdated { get; set; }
    public List<string> Profile { get; set; } = new();
}
