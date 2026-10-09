namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Where an Encounter took place.
/// </summary>
public class EncounterLocation
{
    public ResourceReference Location { get; set; } = new();
}
