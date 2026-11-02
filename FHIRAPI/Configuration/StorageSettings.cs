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

    /// <summary>
    /// Where SMART apps, SMART launches, authorization codes and access tokens are kept:
    /// "EhrApi" (tables PA_SMART_CLIENT, PA_SMART_LAUNCH, PA_SMART_AUTH_CODE, PA_ACCESS_TOKEN through the EHR data API;
    /// needed when the API runs on more than one server) or "InMemory" (one server only, lost on restart; the SMART apps
    /// then come from Security:LocalClients).
    /// </summary>
    public string SmartStore { get; set; } = "InMemory";

    /// <summary>True when the EHR data API store is selected for forms.</summary>
    public bool UseEhrApi => string.Equals(QuestionnaireResponseStore, "EhrApi", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the EHR data API store is selected for the SMART data.</summary>
    public bool UseEhrApiForSmart => string.Equals(SmartStore, "EhrApi", StringComparison.OrdinalIgnoreCase);
}
