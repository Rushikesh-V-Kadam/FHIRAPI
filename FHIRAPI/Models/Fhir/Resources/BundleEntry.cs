namespace FHIRAPI.Models.Fhir;

/// <summary>
/// One entry in a Bundle.
/// </summary>
public class BundleEntry
{
    /// <summary>Absolute URL of the resource, e.g. http://localhost:5100/fhir/r4/Patient/pat-1.</summary>
    public string? FullUrl { get; set; }

    /// <summary>
    /// The resource. Typed as object so it can hold either a model class (Patient, Coverage...) or the raw JSON
    /// of a stored QuestionnaireResponse; the serializer writes whatever the runtime type is.
    /// </summary>
    public object? Resource { get; set; }

    public BundleEntrySearch? Search { get; set; }
}
