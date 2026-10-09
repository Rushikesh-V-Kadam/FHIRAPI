using FHIRAPI.Constants;
using FHIRAPI.Models.Ehr;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Mapping;

/// <summary>
/// Translates the EHR's short code-system names (CPT, LOINC, ICD10CM...) and status values into FHIR.
/// Unknown values are passed through (never silently dropped). Registered as a singleton.
/// </summary>
public class CodeSystemMapper
{
    /// <summary>EHR short name -> FHIR code system URI. Add a line here when the EHR sends a new code system.</summary>
    private static readonly Dictionary<string, string> SystemsByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CPT"] = CodeSystems.Cpt,
        ["HCPCS"] = CodeSystems.Hcpcs,
        ["RXNORM"] = CodeSystems.RxNorm,
        ["ICD10CM"] = CodeSystems.Icd10Cm,
        ["ICD-10-CM"] = CodeSystems.Icd10Cm,
        ["LOINC"] = CodeSystems.Loinc,
        ["SNOMED"] = CodeSystems.Snomed,
        ["NUCC"] = CodeSystems.Nucc,
        ["POS"] = CodeSystems.PlaceOfService,
        ["NPI"] = CodeSystems.Npi,
        ["CDCREC"] = CodeSystems.OmbRaceEthnicity
    };

    /// <summary>
    /// "CPT" -> "http://www.ama-assn.org/go/cpt".
    /// A value that is already a URI is returned unchanged; an unknown name becomes "urn:local:codesystem:{name}".
    /// </summary>
    public string? ToSystemUri(string? codeSystem)
    {
        if (string.IsNullOrWhiteSpace(codeSystem)) return null;
        if (codeSystem.Contains(':')) return codeSystem;
        return SystemsByName.TryGetValue(codeSystem, out var uri) ? uri : $"urn:local:codesystem:{codeSystem.ToLowerInvariant()}";
    }

    /// <summary>"http://loinc.org" -> "LOINC" (used when a FHIR search code is passed to the EHR).</summary>
    public string ToShortName(string systemUri)
    {
        var match = SystemsByName.FirstOrDefault(pair => pair.Value == systemUri);
        return match.Key ?? systemUri;
    }

    /// <summary>
    /// FHIR search codes -> EHR codes list.
    /// "http://loinc.org|39156-5,http://loinc.org|2708-6" -> "LOINC|39156-5,LOINC|2708-6". A code without "|" is kept as is.
    /// </summary>
    public string? ToEhrCodeList(string? fhirCodes)
    {
        if (string.IsNullOrWhiteSpace(fhirCodes)) return null;

        var result = new List<string>();
        foreach (var token in fhirCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bar = token.IndexOf('|');
            if (bar < 0)
            {
                result.Add(token);
                continue;
            }
            var system = token[..bar];
            var code = token[(bar + 1)..];
            result.Add(system.Length == 0 ? $"|{code}" : $"{ToShortName(system)}|{code}");
        }
        return string.Join(",", result);
    }

    /// <summary>Builds a CodeableConcept from an EHR code + short code-system name.</summary>
    public CodeableConcept Concept(string code, string? codeSystem, string? display = null) =>
        new(ToSystemUri(codeSystem), code, display);

    /// <summary>Builds a CodeableConcept from an EhrCode, or null when there is no code.</summary>
    public CodeableConcept? Concept(EhrCode? code, string? defaultSystem = null) =>
        code == null || string.IsNullOrEmpty(code.Code) ? null : Concept(code.Code, code.CodeSystem ?? defaultSystem, code.Display);

    // ------------------------------------------------------------------ value lists

    /// <summary>EHR gender -> male | female | other | unknown.</summary>
    public static string Gender(string? value) => value?.ToLowerInvariant() switch
    {
        "male" or "m" => "male",
        "female" or "f" => "female",
        "other" or "o" => "other",
        _ => "unknown"
    };

    /// <summary>EHR order status -> FHIR request status.</summary>
    public static string RequestStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "draft" => "draft",
        "active" or "signed" => "active",
        "on-hold" => "on-hold",
        "cancelled" or "canceled" => "revoked",
        "completed" => "completed",
        "entered-in-error" => "entered-in-error",
        _ => "unknown"
    };

    /// <summary>EHR priority -> routine | urgent | asap | stat.</summary>
    public static string RequestPriority(string? value) => value?.ToLowerInvariant() switch
    {
        "urgent" => "urgent",
        "stat" => "stat",
        "asap" => "asap",
        _ => "routine"
    };

    /// <summary>EHR encounter class -> v3 ActCode coding.</summary>
    public static Coding EncounterClass(string? value) => value?.ToLowerInvariant() switch
    {
        "inpatient" => new Coding(CodeSystems.ActCode, "IMP", "inpatient encounter"),
        "emergency" => new Coding(CodeSystems.ActCode, "EMER", "emergency"),
        "home" => new Coding(CodeSystems.ActCode, "HH", "home health"),
        "virtual" => new Coding(CodeSystems.ActCode, "VR", "virtual"),
        _ => new Coding(CodeSystems.ActCode, "AMB", "ambulatory")
    };

    /// <summary>EHR encounter status -> FHIR encounter status.</summary>
    public static string EncounterStatus(string? value) => value?.ToLowerInvariant() switch
    {
        "planned" => "planned",
        "arrived" => "arrived",
        "in-progress" => "in-progress",
        "cancelled" or "canceled" => "cancelled",
        "finished" or "completed" => "finished",
        _ => "unknown"
    };

    /// <summary>EHR relationship to subscriber -> subscriber-relationship code.</summary>
    public static CodeableConcept Relationship(string? value)
    {
        var code = value?.ToLowerInvariant() switch
        {
            "spouse" => "spouse",
            "child" => "child",
            "parent" => "parent",
            "other" => "other",
            _ => "self"
        };
        return new CodeableConcept(CodeSystems.SubscriberRelationship, code);
    }
}
