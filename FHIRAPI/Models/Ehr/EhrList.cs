namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Wrapper the EHR uses for every list response: { "items": [...], "total": n }.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrList<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
}
