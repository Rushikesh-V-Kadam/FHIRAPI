namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Why an entry is in a search Bundle.
/// </summary>
public class BundleEntrySearch
{
    /// <summary>"match" = it matched the search; "include" = it was added by _include.</summary>
    public string Mode { get; set; } = "match";
}
