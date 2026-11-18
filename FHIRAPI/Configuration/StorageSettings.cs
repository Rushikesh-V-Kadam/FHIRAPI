namespace FHIRAPI.Configuration;

/// <summary>
/// Chooses where this API keeps its data (appsettings section "Storage").
/// </summary>
public class StorageSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "Storage";

    /// <summary>
    /// "EhrApi" (the EHR's Oracle database, through the EHR data API - appsettings "Ehr") or
    /// "InMemory" (local testing, lost on restart).
    /// </summary>
    public string QuestionnaireResponseStore { get; set; } = "InMemory";

    /// <summary>True when the EHR data API store is selected for forms.</summary>
    public bool UseEhrApi => string.Equals(QuestionnaireResponseStore, "EhrApi", StringComparison.OrdinalIgnoreCase);
}
