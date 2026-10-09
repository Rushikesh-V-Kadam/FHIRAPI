namespace MockEhr.Api.Models;

/// <summary>
/// Extra details of a medication order (orderType = medication). Plain JSON (camelCase), not FHIR.
/// </summary>
public class EhrMedicationDetailsDto
{
    /// <summary>Dose instructions as text.</summary>
    /// <example>4,000 units IV three times a week at dialysis</example>
    public string? DoseText { get; set; }

    /// <summary>Days of supply.</summary>
    /// <example>30</example>
    public int? DaysSupply { get; set; }

    /// <summary>Number of refills.</summary>
    /// <example>2</example>
    public int? Refills { get; set; }

    /// <summary>Pharmacy that dispenses the medication, if known.</summary>
    /// <example>org-2</example>
    public string? PharmacyOrganizationId { get; set; }
}
