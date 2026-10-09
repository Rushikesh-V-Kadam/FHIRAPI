namespace MockEhr.Api.Helpers;

/// <summary>Query-string filters used by the read APIs (same rules as the EHR contract).</summary>
public static class QueryFilter
{
    /// <summary>
    /// "LOINC|39156-5,59408-5" -> true when the code matches one token (and the system too, when the token has one).
    /// An empty filter matches everything.
    /// </summary>
    public static bool CodeMatches(string? filter, string code, string? system)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return true;
        }
        string[] tokens = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string token in tokens)
        {
            int bar = token.IndexOf('|');
            string tokenSystem = bar >= 0 ? token.Substring(0, bar) : string.Empty;
            string tokenCode = bar >= 0 ? token.Substring(bar + 1) : token;
            if (!string.Equals(tokenCode, code, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (tokenSystem.Length == 0 || system == null || string.Equals(tokenSystem, system, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>True when value (an ISO date) is on or after from; an empty from matches everything.</summary>
    public static bool OnOrAfter(string? value, string? from)
    {
        if (string.IsNullOrEmpty(from))
        {
            return true;
        }
        return value != null && string.CompareOrdinal(value, from) >= 0;
    }

    /// <summary>True when filter is empty or equals value (upper / lower case ignored).</summary>
    public static bool SameText(string? filter, string? value)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return true;
        }
        return string.Equals(filter, value, StringComparison.OrdinalIgnoreCase);
    }
}
