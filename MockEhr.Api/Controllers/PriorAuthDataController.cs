using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Models.Rows;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>
/// The EHR data API (mock version). The Payer Gateway and the FHIR API keep ALL their data in the EHR
/// database and read / save it through these endpoints - they have no database connection of their own.
/// The real EHR implements the same endpoints on its Oracle tables (PA_*); this mock keeps the rows in the JSON data file.
///
///   table                       endpoint under api/prior-auth-data                         row class (Models/Rows)
///   PA_PAYER                    GET payers/{insurancePlanId}                             PayerRow
///                               PUT payers/{insurancePlanId} (mock only: lets a demo add a payer; the real EHR team maintains the table)
///   PA_COVERAGE_DECISION        GET / PUT coverage-decisions/{orderId}                     CoverageDecisionRow
///   PA_DTR_LINK                 POST dtr-links,  GET dtr-links/{linkId}                    DtrLinkRow
///   PA_PRIOR_AUTH               GET / PUT prior-auths/{requestId}                          PriorAuthRow
///   PA_CDEX_TASK                PUT cdex-tasks/{insurancePlanId}/{taskId},  GET cdex-tasks?requestId=   CdexTaskRow
///
/// insurancePlanId (a whole number: the EHR's insurance plan id) is the payer key in every table, path and filter.
///   PA_QUESTIONNAIRE_RESPONSE   GET / PUT questionnaire-responses/{id},  GET questionnaire-responses?patientId=   QuestionnaireResponseRow
///   PA_SMART_CLIENT             GET smart-clients/{clientId}  (PUT mock only)                SmartClientRow
///   PA_SMART_LAUNCH             GET / PUT smart-launches/{launchId}                          SmartLaunchRow
///   PA_SMART_AUTH_CODE          PUT / DELETE smart-auth-codes/{codeHash} (DELETE returns the row)   SmartAuthCodeRow
///   PA_ACCESS_TOKEN             GET / PUT access-tokens/{tokenHash}                          AccessTokenRow  
///
/// Rules: a row is a flat JSON object (camelCase, one property per column). The row classes describe every property for
/// Swagger: its meaning, column, example and whether it is always filled.
/// PUT = insert, or update when the key exists (204); the key in the URL wins over the key in the body.
/// GET of a missing row = 404. Lists are plain JSON arrays.
/// </summary>
[ApiController]
[Route("api/prior-auth-data")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class PriorAuthDataController : ControllerBase
{
    /// <summary>JSON settings of a stored row: camelCase, and empty values are kept as null (a row comes back as it was sent).</summary>
    private static readonly JsonSerializerOptions RowJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public PriorAuthDataController(EhrStore store)
    {
        _store = store;
    }

    // ================================================================== PA_PAYER (read only: the EHR team maintains it)

    /// <summary>One payer's configuration by its insurance plan id (table PA_PAYER), in the client EHR's format.</summary>
    /// <remarks>
    /// The gateway calls this for the insurancePlanId of each request and keeps the answer for a few minutes, so a new
    /// or changed payer is picked up without a restart. There is no payer list: the gateway always asks for one payer.
    /// A switched-off payer (Enabled = false) is returned too; the gateway decides.
    /// SQL: WHERE INSURANCE_PLAN_ID = :insurancePlanId.
    ///
    /// The answer is the client EHR's wrapper: { "PayerDetail": { ...the row... }, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.
    /// No such payer = 200 with "PayerDetail": null. The row uses the client's property names (InsurancePlanID, BaseURL ...).
    /// The mock adds the local mock payer (insurancePlanId 1001) the first time the table is empty.
    /// </remarks>
    /// <param name="insurancePlanId" example="1001">The EHR's insurance plan id, e.g. 1001.</param>
    /// <response code="200">The wrapper with the payer row in PayerDetail, or PayerDetail null when there is no such payer.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("payers/{insurancePlanId:int}")]
    [ProducesResponseType(typeof(PayerDetailResponse), StatusCodes.Status200OK)]
    public IActionResult GetPayer(int insurancePlanId)
    {
        PayerDetailResponse answer = new PayerDetailResponse();
        lock (_store.Sync)
        {
            Dictionary<string, JsonElement> payers = Table("payers");
            AddMockPayerWhenEmpty(payers);
            JsonElement row;
            if (payers.TryGetValue(PlanKey(insurancePlanId), out row))
            {
                answer.PayerDetail = ToClientPayerDetail(row);
            }
        }
        return Ok(answer);
    }

    /// <summary>
    /// A stored payer row (camelCase, PayerRow) in the client EHR's property names, e.g. insurancePlanId -> InsurancePlanID,
    /// baseUrl -> BaseURL. A property the row does not have is returned as null.
    /// </summary>
    private static Dictionary<string, JsonElement?> ToClientPayerDetail(JsonElement row)
    {
        string[,] names =
        {
            { "insurancePlanId", "InsurancePlanID" }, { "payerName", "PayerName" }, { "enabled", "Enabled" },
            { "baseUrl", "BaseURL" }, { "cdsServicesPath", "CDSServicesPath" }, { "fhirPath", "FHIRPath" },
            { "authType", "AuthType" }, { "authTokenUrl", "AuthTokenURL" }, { "authClientId", "AuthClientID" },
            { "authClientSecret", "AuthClientSecret" }, { "authScope", "AuthScope" },
            { "signCdsHooksRequests", "SignCDSHooksRequests" }, { "useSubscriptions", "UseSubscriptions" },
            { "notificationSecret", "NotificationSecret" }, { "crdTimeoutSeconds", "CRDTimeoutSeconds" },
            { "timeoutSeconds", "TimeoutSeconds" }, { "dtrMode", "DTRMode" }, { "dtrAppLaunchUrl", "DTRAppLaunchURL" }
        };
        Dictionary<string, JsonElement?> detail = new Dictionary<string, JsonElement?>();
        for (int i = 0; i < names.GetLength(0); i++)
        {
            JsonElement value;
            if (row.ValueKind == JsonValueKind.Object && row.TryGetProperty(names[i, 0], out value))
            {
                detail[names[i, 1]] = value;
            }
            else
            {
                detail[names[i, 1]] = null;
            }
        }
        return detail;
    }

    /// <summary>Mock only: adds or replaces a payer row.</summary>
    /// <remarks>The real EHR team maintains PA_PAYER with its own screens / scripts, so the real EHR does not need this endpoint.</remarks>
    /// <param name="insurancePlanId" example="1001">The EHR's insurance plan id (key). Wins over insurancePlanId in the body.</param>
    /// <param name="row">Required. The payer row.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPut("payers/{insurancePlanId:int}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutPayer(int insurancePlanId, [FromBody, Required] PayerRow row)
    {
        row.InsurancePlanId = insurancePlanId;
        lock (_store.Sync)
        {
            Dictionary<string, JsonElement> payers = Table("payers");
            AddMockPayerWhenEmpty(payers);
            payers[PlanKey(insurancePlanId)] = ToJson(row);
            _store.Save();
        }
        return NoContent();
    }

    /// <summary>Adds the local mock payer when the payer table is empty (first start). Call inside lock (_store.Sync).</summary>
    private void AddMockPayerWhenEmpty(Dictionary<string, JsonElement> payers)
    {
        if (payers.Count == 0)
        {
            payers[PlanKey(SeedData.MockPayerPlanId)] = JsonSerializer.SerializeToElement(SeedData.MockPayerRow(), EhrStore.FileJson);
            _store.Save();
        }
    }

    /// <summary>The key of a payer in the mock's tables: the insurance plan id as text.</summary>
    private static string PlanKey(int insurancePlanId)
    {
        return insurancePlanId.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    // ================================================================== PA_COVERAGE_DECISION

    /// <summary>The latest coverage decision of an order (table PA_COVERAGE_DECISION).</summary>
    /// <param name="orderId" example="ord-1-hd">EHR order id, e.g. ord-1-hd.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No decision is stored for this order. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("coverage-decisions/{orderId}")]
    [ProducesResponseType(typeof(CoverageDecisionRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetCoverageDecision(string orderId)
    {
        return GetRow("coverage-decisions", orderId);
    }

    /// <summary>Saves the coverage decision of an order (table PA_COVERAGE_DECISION).</summary>
    /// <remarks>
    /// Insert, or update when the order already has a decision (one row per order). Called once per order at order-sign.
    /// The EHR order screen reads the same table, so it shows the decision right away.
    /// </remarks>
    /// <param name="orderId" example="ord-1-hd">EHR order id (key). Wins over orderId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("coverage-decisions/{orderId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutCoverageDecision(string orderId, [FromBody, Required] CoverageDecisionRow row)
    {
        row.OrderId = orderId;
        lock (_store.Sync)
        {
            Table("coverage-decisions")[orderId] = ToJson(row);
            ShowCoverageDecisionOnOrder(orderId, row.DecisionJson);
            _store.Save();
        }
        return NoContent();
    }

    // ================================================================== PA_DTR_LINK

    /// <summary>One payer SMART DTR app link (table PA_DTR_LINK).</summary>
    /// <param name="linkId" example="514ff06354c7485a907aec22535743f2">Link id created by the gateway.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No link with this id. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("dtr-links/{linkId}")]
    [ProducesResponseType(typeof(DtrLinkRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetDtrLink(string linkId)
    {
        return GetRow("dtr-links", linkId);
    }

    /// <summary>Saves a new payer SMART DTR app link (table PA_DTR_LINK).</summary>
    /// <remarks>
    /// Insert. The link id is created by the gateway and sent in the body (linkId). Called at order-sign when the payer's
    /// answer has a DTR app link. A link is never changed after it is saved. When the link id already exists (the
    /// gateway retried after a timeout), the row is left as it is and 200 is returned, so a retry never fails.
    /// </remarks>
    /// <param name="row">Required. The row to save; linkId must be filled.</param>
    /// <response code="200">Saved, or already saved (repeated call; nothing changed): { "DtrLinkDetail": the row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing, is not valid JSON, or has no linkId.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPost("dtr-links")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult PostDtrLink([FromBody, Required] DtrLinkRow row)
    {
        if (string.IsNullOrWhiteSpace(row.LinkId))
        {
            return BadRequest(new ErrorResponse("linkId is required."));
        }
        lock (_store.Sync)
        {
            Dictionary<string, JsonElement> links = Table("dtr-links");
            JsonElement existing;
            if (links.TryGetValue(row.LinkId, out existing))
            {
                return Wrapped(DetailName("dtr-links"), existing);   // repeated call: nothing changed
            }
            JsonElement saved = ToJson(row);
            links[row.LinkId] = saved;
            _store.Save();
            return Wrapped(DetailName("dtr-links"), saved);
        }
    }

    // ================================================================== PA_PRIOR_AUTH

    /// <summary>One prior authorization request (table PA_PRIOR_AUTH).</summary>
    /// <param name="requestId" example="PA-2026-0001">The EHR's request id, e.g. PA-2026-0001.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No request with this id. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("prior-auths/{requestId}")]
    [ProducesResponseType(typeof(PriorAuthRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetPriorAuth(string requestId)
    {
        return GetRow("prior-auths", requestId);
    }

    /// <summary>Saves a prior authorization request (table PA_PRIOR_AUTH).</summary>
    /// <remarks>
    /// Insert, or update when the request id exists. Called for a new request, an update, a cancel and every new payer decision
    /// (also by the gateway's background job, days later, when a pended request is decided).
    /// The body can be several MB because lastRequestBundle holds the attached clinical documents.
    /// The EHR prior authorization screen reads the same table, so it shows the decision right away.
    /// </remarks>
    /// <param name="requestId" example="PA-2026-0001">The EHR's request id (key). Wins over requestId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("prior-auths/{requestId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutPriorAuth(string requestId, [FromBody, Required] PriorAuthRow row)
    {
        row.RequestId = requestId;
        JsonElement saved = ToJson(row);
        lock (_store.Sync)
        {
            Table("prior-auths")[requestId] = saved;
            ShowPriorAuthDecision(requestId, row.DecisionJson);
            _store.Save();
        }
        return Wrapped(DetailName("prior-auths"), saved);
    }

    // ================================================================== PA_CDEX_TASK

    /// <summary>Saves a payer information request, a FHIR Task (table PA_CDEX_TASK).</summary>
    /// <remarks>Insert, or update when the Task exists. Called when the payer asks for more documents and each time the Task changes.</remarks>
    /// <param name="insurancePlanId" example="1001">Insurance plan id of the payer (key, part 1). Wins over insurancePlanId in the body.</param>
    /// <param name="taskId" example="5f367f122eba45caba287738b1cc2a38">The payer's Task id (key, part 2). Wins over taskId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("cdex-tasks/{insurancePlanId:int}/{taskId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutCdexTask(int insurancePlanId, string taskId, [FromBody, Required] CdexTaskRow row)
    {
        row.InsurancePlanId = insurancePlanId;
        row.TaskId = taskId;
        return PutRow("cdex-tasks", PlanKey(insurancePlanId) + "|" + taskId, row);
    }

    /// <summary>All Tasks of one prior authorization request (table PA_CDEX_TASK).</summary>
    /// <remarks>SQL: WHERE REQUEST_ID = :requestId.</remarks>
    /// <param name="requestId" example="PA-2026-0001">Required. The EHR's request id, e.g. PA-2026-0001.</param>
    /// <response code="200">The matching rows as a JSON array ([] when the request has no Tasks).</response>
    /// <response code="400">requestId is missing.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("cdex-tasks")]
    [ProducesResponseType(typeof(List<CdexTaskRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult ListCdexTasks([FromQuery, Required] string requestId)
    {
        return ListRows("cdex-tasks", "requestId", requestId);
    }

    // ================================================================== PA_QUESTIONNAIRE_RESPONSE (FHIR API)

    /// <summary>One filled-in DTR form in FHIR format (table PA_QUESTIONNAIRE_RESPONSE).</summary>
    /// <param name="id" example="qr-frm-235b7ad69e85">FHIR id of the form, e.g. qr-frm-235b7ad69e85.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No form with this id. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpGet("questionnaire-responses/{id}")]
    [ProducesResponseType(typeof(QuestionnaireResponseRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetQuestionnaireResponse(string id)
    {
        return GetRow("questionnaire-responses", id);
    }

    /// <summary>Saves a filled-in DTR form in FHIR format (table PA_QUESTIONNAIRE_RESPONSE).</summary>
    /// <remarks>
    /// Insert, or update when the id exists. Called by the FHIR API each time a form is saved.
    /// resourceJson is sent to the payer unchanged, so it must come back exactly as it was saved.
    /// </remarks>
    /// <param name="id" example="qr-frm-235b7ad69e85">FHIR id of the form (key). Wins over id in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpPut("questionnaire-responses/{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutQuestionnaireResponse(string id, [FromBody, Required] QuestionnaireResponseRow row)
    {
        row.Id = id;
        return PutRow("questionnaire-responses", id, row);
    }

    /// <summary>All FHIR forms of one patient (table PA_QUESTIONNAIRE_RESPONSE).</summary>
    /// <remarks>SQL: WHERE PATIENT_ID = :patientId ORDER BY LAST_UPDATED_UTC DESC (newest first).</remarks>
    /// <param name="patientId" example="pat-1">Required. EHR patient id, e.g. pat-1.</param>
    /// <response code="200">The matching rows as a JSON array ([] when the patient has no forms).</response>
    /// <response code="400">patientId is missing.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpGet("questionnaire-responses")]
    [ProducesResponseType(typeof(List<QuestionnaireResponseRow>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult ListQuestionnaireResponses([FromQuery, Required] string patientId)
    {
        return ListRows("questionnaire-responses", "patientId", patientId);
    }

    // ================================================================== PA_SMART_CLIENT (read only: the EHR team maintains it)

    /// <summary>One registered SMART app by its client_id (table PA_SMART_CLIENT).</summary>
    /// <remarks>
    /// Read by the FHIR API at every SMART authorize. SQL: WHERE CLIENT_ID = :clientId.
    /// The mock adds a test app (mock-payer-dtr-app, payer 1001) the first time the table is empty.
    /// </remarks>
    /// <param name="clientId" example="mock-payer-dtr-app">The app's client_id.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No app with this client_id. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpGet("smart-clients/{clientId}")]
    [ProducesResponseType(typeof(SmartClientRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetSmartClient(string clientId)
    {
        lock (_store.Sync)
        {
            AddMockSmartClientWhenEmpty(Table("smart-clients"));
        }
        return GetRow("smart-clients", clientId);
    }

    /// <summary>Mock only: adds or replaces a registered SMART app.</summary>
    /// <remarks>The real EHR team maintains PA_SMART_CLIENT with its own screens / scripts, so the real EHR does not need this endpoint.</remarks>
    /// <param name="clientId" example="mock-payer-dtr-app">The app's client_id (key). Wins over clientId in the body.</param>
    /// <param name="row">Required. The app row.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPut("smart-clients/{clientId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutSmartClient(string clientId, [FromBody, Required] SmartClientRow row)
    {
        row.ClientId = clientId;
        lock (_store.Sync)
        {
            AddMockSmartClientWhenEmpty(Table("smart-clients"));
        }
        return PutRow("smart-clients", clientId, row);
    }

    /// <summary>Adds the test SMART app when the table is empty (first start). Call inside lock (_store.Sync).</summary>
    private void AddMockSmartClientWhenEmpty(Dictionary<string, JsonElement> clients)
    {
        if (clients.Count == 0)
        {
            SmartClientRow row = SeedData.MockSmartClientRow();
            clients[row.ClientId] = ToJson(row);
            _store.Save();
        }
    }

    // ================================================================== PA_SMART_LAUNCH (FHIR API)

    /// <summary>One SMART launch (table PA_SMART_LAUNCH).</summary>
    /// <remarks>SQL: WHERE LAUNCH_ID = :launchId. The FHIR API checks EXPIRES_AT itself.</remarks>
    /// <param name="launchId" example="6-jM9cmaBKMd4rFPAl-hQp_D">The launch id.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No launch with this id. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpGet("smart-launches/{launchId}")]
    [ProducesResponseType(typeof(SmartLaunchRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetSmartLaunch(string launchId)
    {
        return GetRow("smart-launches", launchId);
    }

    /// <summary>Saves a SMART launch (table PA_SMART_LAUNCH). Insert, or update when the id exists.</summary>
    /// <param name="launchId" example="6-jM9cmaBKMd4rFPAl-hQp_D">The launch id (key). Wins over launchId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpPut("smart-launches/{launchId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutSmartLaunch(string launchId, [FromBody, Required] SmartLaunchRow row)
    {
        row.LaunchId = launchId;
        return PutRow("smart-launches", launchId, row);
    }

    // ================================================================== PA_SMART_AUTH_CODE (FHIR API)

    /// <summary>Saves a SMART authorization code (table PA_SMART_AUTH_CODE).</summary>
    /// <param name="codeHash" example="3q2-7wQx0p1mZQ8u0pX6m0nN4l1qkE6m4X0dJ0hKc7Y">Hash of the code (key). Wins over codeHash in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpPut("smart-auth-codes/{codeHash}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutSmartAuthCode(string codeHash, [FromBody, Required] SmartAuthCodeRow row)
    {
        row.CodeHash = codeHash;
        return PutRow("smart-auth-codes", codeHash, row);
    }

    /// <summary>Takes a SMART authorization code: returns the row and deletes it (table PA_SMART_AUTH_CODE).</summary>
    /// <remarks>
    /// Must be ONE step, so two requests can never both get the same code.
    /// Oracle: DELETE FROM PA_SMART_AUTH_CODE WHERE CODE_HASH = :codeHash RETURNING ... INTO ... (no row = 404).
    /// </remarks>
    /// <param name="codeHash" example="3q2-7wQx0p1mZQ8u0pX6m0nN4l1qkE6m4X0dJ0hKc7Y">Hash of the code.</param>
    /// <response code="200">The deleted row.</response>
    /// <response code="404">No such code (unknown or already used). The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpDelete("smart-auth-codes/{codeHash}")]
    [ProducesResponseType(typeof(SmartAuthCodeRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult TakeSmartAuthCode(string codeHash)
    {
        return TakeRow("smart-auth-codes", codeHash);
    }

    // ================================================================== PA_ACCESS_TOKEN (FHIR API)

    /// <summary>One access token by the hash of the token (table PA_ACCESS_TOKEN).</summary>
    /// <remarks>Called on every checked FHIR call. SQL: WHERE TOKEN_HASH = :tokenHash. The FHIR API checks EXPIRES_AT itself.</remarks>
    /// <param name="tokenHash" example="p9w1Cq8m3uYw4mV2Fh0eQk5s7rT6nB1aXz8dLc2jK4E">Hash of the token.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No such token. The answer is 200 with the entity null; 404 only when the path is wrong.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpGet("access-tokens/{tokenHash}")]
    [ProducesResponseType(typeof(AccessTokenRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetAccessToken(string tokenHash)
    {
        return GetRow("access-tokens", tokenHash);
    }

    /// <summary>Saves an access token (table PA_ACCESS_TOKEN). Insert, or update when the hash exists.</summary>
    /// <param name="tokenHash" example="p9w1Cq8m3uYw4mV2Fh0eQk5s7rT6nB1aXz8dLc2jK4E">Hash of the token (key). Wins over tokenHash in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="200">Saved: { "&lt;Entity&gt;Detail": the saved row, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpPut("access-tokens/{tokenHash}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutAccessToken(string tokenHash, [FromBody, Required] AccessTokenRow row)
    {
        row.TokenHash = tokenHash;
        return PutRow("access-tokens", tokenHash, row);
    }

    // ================================================================== helpers

    /// <summary>The rows of one table (key -> row); created when it does not exist yet. Call inside lock (_store.Sync).</summary>
    private Dictionary<string, JsonElement> Table(string name)
    {
        Dictionary<string, JsonElement>? rows;
        if (!_store.Data.GatewayTables.TryGetValue(name, out rows))
        {
            rows = new Dictionary<string, JsonElement>();
            _store.Data.GatewayTables[name] = rows;
        }
        return rows;
    }

    /// <summary>A row class as the JSON that is stored and returned (camelCase, one property per column).</summary>
    private static JsonElement ToJson(object row)
    {
        return JsonSerializer.SerializeToElement(row, row.GetType(), RowJson);
    }

    // CLIENT-EHR: every answer of the data API has the client's format:
    //   { "<EntityName>": <row, list of rows, or null>, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
    // with HTTP 200. "No such row" = the entity is null. The gateway and the FHIR API read the one entity property
    // whatever its name; the names below are the ones in EHR-Data-API-swagger.json.

    /// <summary>Entity name of one row of a table.</summary>
    private static readonly Dictionary<string, string> DetailNames = new Dictionary<string, string>
    {
        { "dtr-links", "DtrLinkDetail" }, { "prior-auths", "PriorAuthDetail" }, { "cdex-tasks", "CdexTaskDetail" },
        { "questionnaire-responses", "QuestionnaireResponseDetail" }, { "smart-clients", "SmartClientDetail" },
        { "smart-launches", "SmartLaunchDetail" }, { "smart-auth-codes", "SmartAuthCodeDetail" },
        { "access-tokens", "AccessTokenDetail" }, { "coverage-decisions", "CoverageDecisionDetail" }
    };

    /// <summary>Entity name of a list of rows of a table.</summary>
    private static readonly Dictionary<string, string> ListNames = new Dictionary<string, string>
    {
        { "cdex-tasks", "CdexTaskList" }, { "questionnaire-responses", "QuestionnaireResponseList" }
    };

    /// <summary>200 with { "&lt;entityName&gt;": entity, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</summary>
    private IActionResult Wrapped(string entityName, object? entity)
    {
        Dictionary<string, object?> answer = new Dictionary<string, object?>();
        answer[entityName] = entity;
        answer["TOKEN_EXPIRED"] = string.Empty;
        answer["ERROR_MESSAGE"] = string.Empty;
        return Ok(answer);
    }

    /// <summary>The entity name of one row of a table.</summary>
    private static string DetailName(string table)
    {
        string? name;
        if (DetailNames.TryGetValue(table, out name))
        {
            return name;
        }
        return "Detail";
    }

    /// <summary>One row, or the entity null when there is no such row.</summary>
    private IActionResult GetRow(string table, string key)
    {
        lock (_store.Sync)
        {
            JsonElement row;
            if (Table(table).TryGetValue(key, out row))
            {
                return Wrapped(DetailName(table), row);
            }
        }
        return Wrapped(DetailName(table), null);
    }

    /// <summary>Inserts or replaces one row; the saved row is returned as the entity.</summary>
    private IActionResult PutRow(string table, string key, object row)
    {
        JsonElement saved = ToJson(row);
        lock (_store.Sync)
        {
            Table(table)[key] = saved;
            _store.Save();
        }
        return Wrapped(DetailName(table), saved);
    }

    /// <summary>Returns one row and deletes it in the same lock; the entity is null when there is no such row.</summary>
    private IActionResult TakeRow(string table, string key)
    {
        lock (_store.Sync)
        {
            JsonElement row;
            if (Table(table).Remove(key, out row))
            {
                _store.Save();
                return Wrapped(DetailName(table), row);
            }
        }
        return Wrapped(DetailName(table), null);
    }

    /// <summary>The rows whose property has this value.</summary>
    private IActionResult ListRows(string table, string property, string value)
    {
        List<JsonElement> result = new List<JsonElement>();
        lock (_store.Sync)
        {
            foreach (JsonElement row in Table(table).Values)
            {
                if (Text(row, property) == value)
                {
                    result.Add(row);
                }
            }
        }
        string? listName;
        if (!ListNames.TryGetValue(table, out listName))
        {
            listName = "List";
        }
        return Wrapped(listName, result);
    }

    /// <summary>A text property of a row, or null.</summary>
    private static string? Text(JsonElement row, string property)
    {
        JsonElement value;
        if (row.ValueKind == JsonValueKind.Object && row.TryGetProperty(property, out value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    /// <summary>
    /// What the real EHR does with a query on PA_COVERAGE_DECISION: the order screen (GET api/orders/{id}/coverage-decision)
    /// shows the decision stored in the decisionJson column. Call inside lock (_store.Sync).
    /// </summary>
    private void ShowCoverageDecisionOnOrder(string orderId, string? decisionJson)
    {
        if (string.IsNullOrEmpty(decisionJson))
        {
            return;
        }
        CoverageDecisionDto? decision = JsonSerializer.Deserialize<CoverageDecisionDto>(decisionJson, EhrStore.FileJson);
        if (decision == null)
        {
            return;
        }
        decision.OrderId = orderId;

        bool replaced = false;
        for (int i = 0; i < _store.Data.CoverageDecisions.Count; i++)
        {
            if (_store.Data.CoverageDecisions[i].OrderId == orderId)
            {
                _store.Data.CoverageDecisions[i] = decision;
                replaced = true;
                break;
            }
        }
        if (!replaced)
        {
            _store.Data.CoverageDecisions.Add(decision);
        }
        _store.LogWriteBack("coverage-decision", orderId, decision);
    }

    /// <summary>
    /// What the real EHR does with a query on PA_PRIOR_AUTH: the prior auth screen (GET api/prior-auth-requests)
    /// shows the decision stored in the decisionJson column. Call inside lock (_store.Sync).
    /// </summary>
    private void ShowPriorAuthDecision(string requestId, string? decisionJson)
    {
        if (string.IsNullOrEmpty(decisionJson))
        {
            return;
        }
        PriorAuthDecisionDto? decision = JsonSerializer.Deserialize<PriorAuthDecisionDto>(decisionJson, EhrStore.FileJson);
        if (decision == null)
        {
            return;
        }
        decision.RequestId = requestId;

        bool replaced = false;
        for (int i = 0; i < _store.Data.PriorAuthDecisions.Count; i++)
        {
            if (_store.Data.PriorAuthDecisions[i].RequestId == requestId)
            {
                _store.Data.PriorAuthDecisions[i] = decision;
                replaced = true;
                break;
            }
        }
        if (!replaced)
        {
            _store.Data.PriorAuthDecisions.Add(decision);
        }
        _store.LogWriteBack("prior-auth-decision", requestId, decision);
    }
}
