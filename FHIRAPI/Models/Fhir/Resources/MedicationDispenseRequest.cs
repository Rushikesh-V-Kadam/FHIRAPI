namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Dispense details of a MedicationRequest: quantity, days supply, refills, pharmacy.
/// </summary>
public class MedicationDispenseRequest
{
    public int? NumberOfRepeatsAllowed { get; set; }
    public Quantity? Quantity { get; set; }
    public Quantity? ExpectedSupplyDuration { get; set; }
    /// <summary>Pharmacy (Organization).</summary>
    public ResourceReference? Performer { get; set; }
}
