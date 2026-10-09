namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Reference, e.g. { "reference": "Patient/pat-1" }.
/// Named ResourceReference because a property called "Reference" cannot live in a class called Reference.
/// </summary>
public class ResourceReference
{
    /// <summary>Relative reference "Type/id".</summary>
    public string? Reference { get; set; }

    /// <summary>Business identifier, used when there is no FHIR id (e.g. the payer on Coverage.payor).</summary>
    public Identifier? Identifier { get; set; }

    public string? Display { get; set; }

    /// <summary>Empty reference (used by the JSON serializer).</summary>
    public ResourceReference() { }

    /// <summary>Creates "Type/id".</summary>
    public ResourceReference(string resourceType, string id)
    {
        Reference = $"{resourceType}/{id}";
    }

    /// <summary>Resource type part of "Type/id", or null.</summary>
    public string? GetResourceType()
    {
        var parts = Reference?.Split('/');
        return parts is { Length: >= 2 } ? parts[^2] : null;
    }

    /// <summary>Id part of "Type/id", or null.</summary>
    public string? GetId() => Reference?.Split('/').LastOrDefault();
}
