namespace FHIRAPI.Configuration;

/// <summary>
/// Settings for calling the EHR REST APIs (appsettings section "Ehr").
/// </summary>
public class EhrSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "Ehr";

    /// <summary>Base URL of the EHR REST APIs. Local mock EHR: http://localhost:5090.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5090";

    /// <summary>
    /// Path of the EHR data API under BaseUrl (default "api/prior-auth-data"). DTR forms are stored through it
    /// in the EHR database (table PA_QUESTIONNAIRE_RESPONSE) when Storage:QuestionnaireResponseStore is "EhrApi".
    /// </summary>
    public string DataPath { get; set; } = "api/prior-auth-data";


    /// <summary>How long to wait for the EHR before giving up.</summary>
    public int TimeoutSeconds { get; set; } = 300;
}
