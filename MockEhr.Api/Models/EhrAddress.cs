namespace MockEhr.Api.Models;

/// <summary>
/// A postal address (patient, organization or location). Plain JSON (camelCase), not FHIR.
/// </summary>
public class EhrAddress
{
    /// <summary>Street address.</summary>
    /// <example>100 Demo Street</example>
    public string? Line1 { get; set; }

    /// <summary>Apartment, suite ... if any.</summary>
    /// <example>Suite 200</example>
    public string? Line2 { get; set; }

    /// <summary>City.</summary>
    /// <example>Dallas</example>
    public string? City { get; set; }

    /// <summary>State, 2 letters.</summary>
    /// <example>TX</example>
    public string? State { get; set; }

    /// <summary>ZIP code.</summary>
    /// <example>75201</example>
    public string? PostalCode { get; set; }

    /// <summary>Country, 2 letters.</summary>
    /// <example>US</example>
    public string? Country { get; set; }
}
