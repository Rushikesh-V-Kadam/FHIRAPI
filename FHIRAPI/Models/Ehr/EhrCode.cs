namespace FHIRAPI.Models.Ehr;

/// <summary>
/// A code as the EHR sends it: code + short code-system name (e.g. "ICD10CM") + display.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrCode
{
    public string Code { get; set; } = string.Empty;
    public string? CodeSystem { get; set; }
    public string? Display { get; set; }
}
