namespace MockEhr.Api.Constants;

/// <summary>
/// Swagger sections of the mock EHR API, by WHO calls each endpoint. Used with [Tags(...)] on every action.
/// An endpoint with two callers carries two tags and shows in both sections.
/// </summary>
public static class SwaggerGroups
{
    /// <summary>EHR data API (api/prior-auth-data): the Payer Gateway saves and reads its PA_* tables here.</summary>
    public const string GatewayData = "1. Data API - called by Payer Gateway (PA_* tables)";

    /// <summary>EHR data API: the FHIR API stores the DTR forms (PA_QUESTIONNAIRE_RESPONSE) here.</summary>
    public const string FhirApiData = "2. Data API - called by FHIR API (DTR forms)";

    /// <summary>EHR read APIs the FHIR API turns into FHIR resources (and the copy of a form in the chart).</summary>
    public const string FhirApiRead = "3. EHR API - called by FHIR API (patient data as FHIR)";

    /// <summary>What the Mock EHR UI (React) uses to show and add / update test data.</summary>
    public const string MockUi = "4. Mock EHR UI - show, add and update test data";

    /// <summary>Old write-back targets of the gateway. Nothing calls them since the shared database.</summary>
    public const string Unused = "5. Not used any more (old write-back)";

    /// <summary>
    /// True for sections 1 to 3: what the client's real EHR must provide. These are the sections of the Swagger
    /// document for the EHR team (/swagger/ehr-team/swagger.json). Sections 4 and 5 exist only in this mock.
    /// </summary>
    public static bool IsForEhrTeam(string? section)
    {
        return section == GatewayData || section == FhirApiData || section == FhirApiRead;
    }
}
