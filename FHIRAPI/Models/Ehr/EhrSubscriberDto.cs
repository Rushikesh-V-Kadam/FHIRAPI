namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Policy holder of an insurance, when it is not the patient.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrSubscriberDto
{
    public string? MemberId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DateOfBirth { get; set; }
}
