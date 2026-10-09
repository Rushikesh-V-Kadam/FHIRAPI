namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Coding: one code from one code system, e.g. system "http://loinc.org", code "39156-5".
/// </summary>
public class Coding
{
    public string? System { get; set; }
    public string? Code { get; set; }
    public string? Display { get; set; }

    /// <summary>Empty coding (used by the JSON serializer).</summary>
    public Coding() { }

    /// <summary>Creates a coding with system, code and optional display text.</summary>
    public Coding(string? system, string code, string? display = null)
    {
        System = system;
        Code = code;
        Display = display;
    }
}
