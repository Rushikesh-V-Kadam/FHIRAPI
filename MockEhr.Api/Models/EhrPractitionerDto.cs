namespace MockEhr.Api.Models;

/// <summary>
/// A clinician, as the EHR returns it from GET api/practitioners/{practitionerId}. Plain JSON (camelCase), not FHIR.
/// The FHIR API turns it into a FHIR Practitioner and PractitionerRole.
/// </summary>
public class EhrPractitionerDto
{
    /// <summary>EHR practitioner id.</summary>
    /// <example>prac-1</example>
    public string PractitionerId { get; set; } = string.Empty;

    /// <summary>National Provider Identifier (10 digits). Needed for prior authorization requests (PAS).</summary>
    /// <example>1111111112</example>
    public string? Npi { get; set; }

    /// <summary>Given name.</summary>
    /// <example>Maria</example>
    public string? FirstName { get; set; }

    /// <summary>Family name.</summary>
    /// <example>Santos</example>
    public string? LastName { get; set; }

    /// <summary>Title.</summary>
    /// <example>Dr.</example>
    public string? Prefix { get; set; }

    /// <summary>Specialty as a NUCC taxonomy code (codeSystem NUCC), e.g. 207RN0300X Nephrology.</summary>
    public EhrCode? Specialty { get; set; }

    /// <summary>Organization the clinician works for.</summary>
    /// <example>org-1</example>
    public string? OrganizationId { get; set; }

    /// <summary>Phone.</summary>
    /// <example>214-555-0201</example>
    public string? Phone { get; set; }

    /// <summary>Fax.</summary>
    /// <example>214-555-0291</example>
    public string? Fax { get; set; }

    /// <summary>false = no longer active. Empty = true.</summary>
    /// <example>true</example>
    public bool? Active { get; set; }
}
