using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Encounter (US Core): a visit or admission.
/// </summary>
public class Encounter : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Encounter";

    public string Status { get; set; } = "finished";
    /// <summary>AMB, IMP, EMER, HH, VR (v3 ActCode).</summary>
    public Coding? Class { get; set; }
    public List<CodeableConcept> Type { get; set; } = new();
    public ResourceReference? Subject { get; set; }
    public List<EncounterParticipant> Participant { get; set; } = new();
    public Period? Period { get; set; }
    public List<CodeableConcept> ReasonCode { get; set; } = new();
    public EncounterHospitalization? Hospitalization { get; set; }
    public List<EncounterLocation> Location { get; set; } = new();
    /// <summary>The organization responsible for the encounter.</summary>
    public ResourceReference? ServiceProvider { get; set; }
}
