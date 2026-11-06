using System.Globalization;

namespace FHIRAPI.Helpers;

// CLIENT-EHR: the client's EHR sends date-times without a time zone (e.g. "2026-04-20T00:00:00"), and the client says
// they are US Central time. FHIR R4 needs a time zone on every dateTime that has a time, so the zone is added here.
/// <summary>
/// Turns an EHR date-time into a FHIR dateTime: a value without a zone is taken as US Central time and gets its offset
/// (-06:00 in standard time, -05:00 in daylight saving time), e.g. "2026-04-20T00:00:00" -> "2026-04-20T00:00:00-05:00".
/// A date only ("2026-04-20") and a value that already has a zone ("...Z", "...-05:00") are kept as they are.
/// </summary>
public static class EhrDateTime
{
    /// <summary>
    /// The EHR's time zone. "America/Chicago" works on Windows and Linux with .NET 6 or later; the Windows name is the
    /// fallback.
    /// TODO: confirm with the client that every EHR date-time is US Central time.
    /// </summary>
    private const string CentralTimeZoneId = "America/Chicago";

    /// <summary>Windows name of the same time zone.</summary>
    private const string CentralTimeZoneWindowsId = "Central Standard Time";

    /// <summary>Date-time forms the client sends without a zone.</summary>
    private static readonly string[] LocalFormats =
    {
        "yyyy-MM-dd'T'HH:mm:ss",
        "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
        "yyyy-MM-dd'T'HH:mm"
    };

    /// <summary>Loaded once at start.</summary>
    private static readonly TimeZoneInfo Central = FindCentral();

    /// <summary>
    /// The FHIR dateTime for an EHR value, or null when the value is empty or not a date-time that can be read
    /// (a wrong value is left out rather than sent as an invalid FHIR dateTime).
    /// </summary>
    public static string? ToFhir(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        string text = value.Trim();

        // A date only: valid FHIR as it is.
        DateTime dateOnly;
        if (text.Length == 10 && DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOnly))
        {
            return text;
        }

        // Already has a zone (Z or +hh:mm / -hh:mm after the time): keep it.
        if (HasZone(text))
        {
            DateTimeOffset withZone;
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out withZone))
            {
                return text;
            }
            return null;
        }

        DateTime local;
        if (!DateTime.TryParseExact(text, LocalFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out local))
        {
            return null;
        }
        TimeSpan offset = Central.GetUtcOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified));
        DateTimeOffset central = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset);
        return central.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture);
    }

    /// <summary>True when the text ends with Z or with an offset after the time part.</summary>
    private static bool HasZone(string text)
    {
        if (text.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        int timeStart = text.IndexOf('T');
        if (timeStart < 0)
        {
            return false;
        }
        string time = text.Substring(timeStart + 1);
        return time.Contains('+') || time.Contains('-');
    }

    /// <summary>US Central time zone (IANA name first, then the Windows name).</summary>
    private static TimeZoneInfo FindCentral()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(CentralTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(CentralTimeZoneWindowsId);
        }
    }
}
