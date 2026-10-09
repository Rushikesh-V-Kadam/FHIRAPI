namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Admission details of an Encounter (only the discharge disposition is used).
/// </summary>
public class EncounterHospitalization
{
    public CodeableConcept? DischargeDisposition { get; set; }
}
