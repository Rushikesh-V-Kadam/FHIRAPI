using System.Text.Json;
using MockEhr.Api.Models;

namespace MockEhr.Api.Storage;

/// <summary>
/// Everything the mock EHR stores. The whole object is saved as one JSON file (MockEhr:DataFile,
/// default Data/ehr-data.json). On the first start the file does not exist and SeedData fills it.
/// </summary>
public class EhrData
{
    public List<EhrPatientDto> Patients { get; set; } = new List<EhrPatientDto>();
    public List<EhrInsuranceDto> Insurances { get; set; } = new List<EhrInsuranceDto>();
    public List<EhrPractitionerDto> Practitioners { get; set; } = new List<EhrPractitionerDto>();
    public List<EhrOrganizationDto> Organizations { get; set; } = new List<EhrOrganizationDto>();
    public List<EhrLocationDto> Locations { get; set; } = new List<EhrLocationDto>();
    public List<EhrOrderDto> Orders { get; set; } = new List<EhrOrderDto>();
    public List<EhrAppointmentDto> Appointments { get; set; } = new List<EhrAppointmentDto>();
    public List<EhrEncounterDto> Encounters { get; set; } = new List<EhrEncounterDto>();
    public List<EhrObservationDto> Observations { get; set; } = new List<EhrObservationDto>();
    public List<EhrConditionDto> Conditions { get; set; } = new List<EhrConditionDto>();
    public List<EhrProcedureDto> Procedures { get; set; } = new List<EhrProcedureDto>();
    public List<EhrDocumentDto> Documents { get; set; } = new List<EhrDocumentDto>();
    public List<EhrDocumentContentDto> DocumentContent { get; set; } = new List<EhrDocumentContentDto>();

    // ------------------------------------------------------------ written by the Payer Gateway / FHIR API
    public List<CoverageDecisionDto> CoverageDecisions { get; set; } = new List<CoverageDecisionDto>();
    public List<QuestionnaireResponseDto> QuestionnaireResponses { get; set; } = new List<QuestionnaireResponseDto>();
    public List<PriorAuthDecisionDto> PriorAuthDecisions { get; set; } = new List<PriorAuthDecisionDto>();
    public List<WriteBack> WriteBacks { get; set; } = new List<WriteBack>();

    /// <summary>Last number used for QuestionnaireResponse ids (qr-1, qr-2 ...).</summary>
    public int QrCounter { get; set; }

    // ------------------------------------------------------------ the gateway's tables (EHR data API, PriorAuthDataController)
    /// <summary>
    /// Stands in for the PA_* Oracle tables of the real EHR: table name (e.g. "prior-auths") -> row key -> row.
    /// The Payer Gateway and the FHIR API read and save these rows through api/prior-auth-data.
    /// </summary>
    public Dictionary<string, Dictionary<string, JsonElement>> GatewayTables { get; set; } = new Dictionary<string, Dictionary<string, JsonElement>>();
}
