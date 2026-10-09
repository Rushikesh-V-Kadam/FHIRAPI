namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Plan or group number on a Coverage.
/// </summary>
public class CoverageClass
{
    /// <summary>"plan" or "group" (coverage-class code system).</summary>
    public CodeableConcept? Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? Name { get; set; }
}
