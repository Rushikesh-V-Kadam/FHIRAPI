namespace MockEhr.Api.Models;

/// <summary>
/// A coded value: code + code system + text. Plain JSON (camelCase), not FHIR.
/// codeSystem is a short name the FHIR API knows: CPT, HCPCS, RXNORM, ICD10CM, LOINC, SNOMED, NUCC, POS, NPI, CDCREC
/// (a full URI such as http://loinc.org is also accepted).
/// </summary>
public class EhrCode
{
    /// <summary>The code.</summary>
    /// <example>N18.6</example>
    public string Code { get; set; } = string.Empty;

    /// <summary>Short name of the code system (CPT, HCPCS, RXNORM, ICD10CM, LOINC, SNOMED, NUCC, POS, CDCREC) or a full URI.</summary>
    /// <example>ICD10CM</example>
    public string? CodeSystem { get; set; }

    /// <summary>Text of the code.</summary>
    /// <example>End stage renal disease</example>
    public string? Display { get; set; }
}
