using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// One entry of the SMART "fhirContext" array in the token response.
/// </summary>
public class FhirContextItem
{
    /// <summary>"Type/id" of a resource the form is for.</summary>
    /// <example>ServiceRequest/ord-1-hd</example>
    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
}
