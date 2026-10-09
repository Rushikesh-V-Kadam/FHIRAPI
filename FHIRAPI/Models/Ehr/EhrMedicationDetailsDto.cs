namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Extra details of a medication order (dose, days supply, refills, pharmacy).
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrMedicationDetailsDto
{
    public string? DoseText { get; set; }
    public int? DaysSupply { get; set; }
    public int? Refills { get; set; }
    public string? PharmacyOrganizationId { get; set; }
}
