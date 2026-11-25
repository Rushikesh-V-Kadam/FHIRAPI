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

    // TODO: client EHR paths of the DTR form APIs (table PA_QUESTIONNAIRE_RESPONSE); change here if the client moves them.
    /// <summary>
    /// Path under BaseUrl of the client's "get questionnaire response" API (GET ...?responseID={id}).
    /// Used when Storage:QuestionnaireResponseStore is "EhrApi".
    /// </summary>
    public string QuestionnaireGetPath { get; set; } = "Enterprise/getpasquestdtl";

    /// <summary>
    /// Path under BaseUrl of the client's "save questionnaire response" API (POST, insert or update by ResponseID).
    /// Used when Storage:QuestionnaireResponseStore is "EhrApi".
    /// </summary>
    public string QuestionnaireSavePath { get; set; } = "Enterprise/savepasquest";

    /// <summary>How long to wait for the EHR before giving up.</summary>
    public int TimeoutSeconds { get; set; } = 300;
}
