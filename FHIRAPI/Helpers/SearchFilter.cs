using System.Globalization;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Helpers;

// CLIENT-EHR: the client's list APIs (getproclst, getconlst, getobslst) take only patientID and return every row of the
// patient, so the FHIR search parameters code, date, category and clinical-status are applied here.
/// <summary>
/// Applies FHIR search parameters to resources already built from EHR rows.
/// An empty parameter matches everything.
/// </summary>
public static class SearchFilter
{
    /// <summary>
    /// code search: a comma separated list of "code" or "system|code" (e.g. "http://loinc.org|2502-3,2160-0").
    /// A token with a system matches a coding with that system, or a coding that has no system (the client sends codes
    /// without one); a token "|code" matches only a coding without a system.
    /// </summary>
    public static bool CodeMatches(CodeableConcept? concept, string? fhirCodes)
    {
        if (string.IsNullOrWhiteSpace(fhirCodes))
        {
            return true;
        }
        if (concept == null)
        {
            return false;
        }
        string[] tokens = fhirCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string token in tokens)
        {
            string? system = null;
            string code = token;
            int bar = token.IndexOf('|');
            if (bar >= 0)
            {
                system = token.Substring(0, bar);
                code = token.Substring(bar + 1);
            }
            foreach (Coding coding in concept.Coding)
            {
                if (!string.Equals(coding.Code, code, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (system == null)
                {
                    return true;
                }
                if (system.Length == 0 && string.IsNullOrEmpty(coding.System))
                {
                    return true;
                }
                if (system.Length > 0 && (string.IsNullOrEmpty(coding.System) || string.Equals(coding.System, system, StringComparison.Ordinal)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// date search "ge"/"gt" (the from value of FhirSearchParameters.GetDateFrom): true when the resource date is on or
    /// after it. A date-only from value is compared by date; a full date-time by instant. A resource without a date does
    /// not match a date search.
    /// </summary>
    public static bool OnOrAfter(string? fhirDateTime, string? from)
    {
        if (string.IsNullOrWhiteSpace(from))
        {
            return true;
        }
        if (string.IsNullOrWhiteSpace(fhirDateTime))
        {
            return false;
        }
        string fromText = from.Trim();
        if (fromText.Length == 10)
        {
            if (fhirDateTime.Length < 10)
            {
                return false;
            }
            return string.CompareOrdinal(fhirDateTime.Substring(0, 10), fromText) >= 0;
        }
        DateTimeOffset value;
        DateTimeOffset start;
        if (!DateTimeOffset.TryParse(fhirDateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value))
        {
            return false;
        }
        if (!DateTimeOffset.TryParse(fromText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out start))
        {
            return true;   // a from value that cannot be read does not filter
        }
        return value >= start;
    }

    /// <summary>
    /// A token search on a list of concepts (Observation.category, Condition.category): "code" or "system|code".
    /// </summary>
    public static bool AnyCodeMatches(List<CodeableConcept> concepts, string? fhirCodes)
    {
        if (string.IsNullOrWhiteSpace(fhirCodes))
        {
            return true;
        }
        foreach (CodeableConcept concept in concepts)
        {
            if (CodeMatches(concept, fhirCodes))
            {
                return true;
            }
        }
        return false;
    }
}
