using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR PractitionerRole (US Core). The EHR has no separate role record, so this is built from the
/// practitioner record and gets the id "role-{practitionerId}".
/// </summary>
public class PractitionerRole : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "PractitionerRole";

    public ResourceReference? Practitioner { get; set; }
    public ResourceReference? Organization { get; set; }
    public List<CodeableConcept> Specialty { get; set; } = new();
}
