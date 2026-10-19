namespace FHIRAPI.Helpers;

/// <summary>
/// Checks US National Provider Identifiers (NPI). An NPI is 10 digits; the last digit is a check digit computed with
/// the Luhn formula over the prefix 80840 and the first 9 digits (CMS NPI standard).
/// </summary>
public static class NpiNumbers
{
    /// <summary>Prefix CMS adds in front of the 9 NPI digits before the Luhn check.</summary>
    private const string LuhnPrefix = "80840";

    /// <summary>True when the text is a well-formed NPI: 10 digits with a correct check digit.</summary>
    public static bool IsValid(string? value)
    {
        if (value == null || value.Length != 10)
        {
            return false;
        }
        foreach (char c in value)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        // Luhn over "80840" + all 10 digits: walking from the right, every second digit is doubled.
        string digits = LuhnPrefix + value;
        int sum = 0;
        bool doubleIt = false;
        for (int i = digits.Length - 1; i >= 0; i--)
        {
            int digit = digits[i] - '0';
            if (doubleIt)
            {
                digit = digit * 2;
                if (digit > 9)
                {
                    digit = digit - 9;
                }
            }
            sum = sum + digit;
            doubleIt = !doubleIt;
        }
        return sum % 10 == 0;
    }
}
