namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Practitioner taking part in an Encounter.
/// </summary>
public class EncounterParticipant
{
    public ResourceReference? Individual { get; set; }
}
