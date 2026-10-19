namespace FHIRAPI.Helpers;

// CLIENT-EHR: the client's organization has no NPI; each facility (location) has one, so each location is also served
// as a facility Organization with that NPI.
/// <summary>
/// Ids of facility Organizations. The client's EHR keeps each facility (its own NPI, name and address) as a location,
/// and only the parent organization (tax id, no NPI) as an organization. FHIR needs the facility as an Organization
/// too: a PAS Claim.provider can only point to an Organization (or a practitioner), never to a Location. So every EHR
/// location is also served as Organization/facility-{locationId}.
/// </summary>
public static class FacilityIds
{
    /// <summary>Start of every facility Organization id.</summary>
    public const string Prefix = "facility-";

    /// <summary>Organization id of the facility of this EHR location, e.g. "loc-main" -> "facility-loc-main".</summary>
    public static string ToOrganizationId(string locationId)
    {
        return Prefix + locationId;
    }

    /// <summary>True when the Organization id is a facility id (built from an EHR location).</summary>
    public static bool IsFacility(string organizationId)
    {
        return organizationId.StartsWith(Prefix, StringComparison.Ordinal) && organizationId.Length > Prefix.Length;
    }

    /// <summary>The EHR location id inside a facility Organization id, e.g. "facility-loc-main" -> "loc-main".</summary>
    public static string ToLocationId(string organizationId)
    {
        return organizationId.Substring(Prefix.Length);
    }
}
