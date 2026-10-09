namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Identifier: a business id such as an MRN, NPI or member id.
/// </summary>
public class Identifier
{
    /// <summary>Kind of identifier, e.g. v2-0203 "MR" (medical record number).</summary>
    public CodeableConcept? Type { get; set; }

    /// <summary>Namespace of the value, e.g. http://hl7.org/fhir/sid/us-npi.</summary>
    public string? System { get; set; }

    public string? Value { get; set; }

    /// <summary>Empty identifier (used by the JSON serializer).</summary>
    public Identifier() { }

    /// <summary>Creates an identifier with system and value.</summary>
    public Identifier(string system, string value)
    {
        System = system;
        Value = value;
    }
}
