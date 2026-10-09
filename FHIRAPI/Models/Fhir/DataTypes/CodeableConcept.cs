namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR CodeableConcept: a concept expressed as one or more codings plus optional plain text.
/// </summary>
public class CodeableConcept
{
    public List<Coding> Coding { get; set; } = new();
    public string? Text { get; set; }

    /// <summary>Empty concept (used by the JSON serializer).</summary>
    public CodeableConcept() { }

    /// <summary>Creates a concept with a single coding.</summary>
    public CodeableConcept(string? system, string code, string? display = null)
    {
        Coding.Add(new Coding(system, code, display));
    }
}
