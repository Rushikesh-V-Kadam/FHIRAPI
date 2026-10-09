namespace MockEhr.Api.Models;

/// <summary>
/// The subscriber of an insurance, when it is not the patient. Plain JSON (camelCase), not FHIR.
/// </summary>
public class EhrSubscriberDto
{
    /// <summary>The subscriber's member id.</summary>
    /// <example>MBR-1000</example>
    public string? MemberId { get; set; }

    /// <summary>Given name.</summary>
    /// <example>Linda</example>
    public string? FirstName { get; set; }

    /// <summary>Family name.</summary>
    /// <example>Hayes</example>
    public string? LastName { get; set; }

    /// <summary>Date of birth, yyyy-MM-dd.</summary>
    /// <example>1961-07-02</example>
    public string? DateOfBirth { get; set; }
}
