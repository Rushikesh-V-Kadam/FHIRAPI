namespace MockEhr.Api.Models;

/// <summary>
/// A place where care is given, as the EHR returns it from GET api/locations/{locationId}.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Location.
/// </summary>
public class EhrLocationDto
{
    /// <summary>EHR location id.</summary>
    /// <example>loc-main</example>
    public string LocationId { get; set; } = string.Empty;

    /// <summary>Name of the location.</summary>
    /// <example>Demo Dialysis Center - Main, in-center unit</example>
    public string? Name { get; set; }

    /// <summary>CMS place of service code, e.g. 65 = ESRD treatment facility, 11 = office, 12 = home.</summary>
    /// <example>65</example>
    public string? PlaceOfServiceCode { get; set; }

    /// <summary>Address of the location.</summary>
    public EhrAddress? Address { get; set; }

    /// <summary>Organization that runs the location.</summary>
    /// <example>org-1</example>
    public string? OrganizationId { get; set; }
}
