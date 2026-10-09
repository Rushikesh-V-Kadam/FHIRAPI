namespace MockEhr.Api.Models;

/// <summary>
/// A provider organization (dialysis center, clinic), as the EHR returns it from GET api/organizations/{organizationId}.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Organization.
/// </summary>
public class EhrOrganizationDto
{
    /// <summary>EHR organization id. Used as requestingOrganizationId in prior authorization requests.</summary>
    /// <example>org-1</example>
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>National Provider Identifier (10 digits). Needed for prior authorization requests (PAS).</summary>
    /// <example>1999999992</example>
    public string? Npi { get; set; }

    /// <summary>Tax id (EIN).</summary>
    /// <example>12-3456789</example>
    public string? TaxId { get; set; }

    /// <summary>Name of the organization.</summary>
    /// <example>Demo Dialysis Center - Main</example>
    public string? Name { get; set; }

    /// <summary>Address of the organization.</summary>
    public EhrAddress? Address { get; set; }

    /// <summary>Phone.</summary>
    /// <example>214-555-0300</example>
    public string? Phone { get; set; }

    /// <summary>false = no longer active. Empty = true.</summary>
    /// <example>true</example>
    public bool? Active { get; set; }
}
