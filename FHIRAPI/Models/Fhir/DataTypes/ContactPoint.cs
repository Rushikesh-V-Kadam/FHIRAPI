namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR ContactPoint: phone number or email address.
/// </summary>
public class ContactPoint
{
    /// <summary>phone | email | fax ...</summary>
    public string? System { get; set; }
    public string? Value { get; set; }
    /// <summary>home | work | mobile ...</summary>
    public string? Use { get; set; }
}
