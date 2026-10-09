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
///   PA_PAYER                    GET payers                                                 PayerRow
///                               PUT payers/{payerName} (mock only: lets a demo add a payer; the real EHR team maintains the table)
///   PA_CRD_EVENT                GET / PUT crd-events/{eventId}                             CrdEventRow
///   PA_COVERAGE_DECISION        GET / PUT coverage-decisions/{orderId}                     CoverageDecisionRow
///   PA_DTR_FORM                 GET / PUT dtr-forms/{formId}                               DtrFormRow
///   PA_DTR_LINK                 GET / PUT dtr-links/{linkId}                               DtrLinkRow
///   PA_PRIOR_AUTH               GET / PUT prior-auths/{requestId},  GET prior-auths?status=&amp;payerName=   PriorAuthRow
///   PA_PAYER_SUBSCRIPTION       GET / PUT payer-subscriptions/{payerName}/{organizationNpi} PayerSubscriptionRow
///   PA_CDEX_TASK                PUT cdex-tasks/{payerName}/{taskId},  GET cdex-tasks?requestId=   CdexTaskRow
///   PA_QUESTIONNAIRE_RESPONSE   GET / PUT questionnaire-responses/{id},  GET questionnaire-responses?patientId=   QuestionnaireResponseRow
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

    /// <summary>All payers (table PA_PAYER), enabled or not.</summary>
    /// <remarks>
    /// The gateway reads the list at start-up and every few minutes, so a new or changed payer is picked up without a restart.
    /// The EHR team maintains the table; the gateway never changes it.
    /// The mock adds the local mock payer the first time the list is empty.
    /// </remarks>
    /// <response code="200">All rows as a JSON array ([] when the table is empty).</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("payers")]
    [ProducesResponseType(typeof(List<PayerRow>), StatusCodes.Status200OK)]
    public IActionResult GetPayers()
    {
        lock (_store.Sync)
        {
            Dictionary<string, JsonElement> payers = Table("payers");
            if (payers.Count == 0)
            {
                payers["mockpayer"] = JsonSerializer.SerializeToElement(SeedData.MockPayerRow(), EhrStore.FileJson);
                _store.Save();
            }
            return Ok(new List<JsonElement>(payers.Values));
        }
    }

    /// <summary>Mock only: adds or replaces a payer row.</summary>
    /// <remarks>The real EHR team maintains PA_PAYER with its own screens / scripts, so the real EHR does not need this endpoint.</remarks>
    /// <param name="payerName" example="MockPayer">Payer name (key). Wins over payerName in the body.</param>
    /// <param name="row">Required. The payer row.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPut("payers/{payerName}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutPayer(string payerName, [FromBody, Required] PayerRow row)
    {
        row.PayerName = payerName;
        lock (_store.Sync)
        {
            Dictionary<string, JsonElement> payers = Table("payers");
            if (payers.Count == 0)
            {
                payers["mockpayer"] = JsonSerializer.SerializeToElement(SeedData.MockPayerRow(), EhrStore.FileJson);
            }
            payers[payerName.ToLowerInvariant()] = ToJson(row);
            _store.Save();
        }
        return NoContent();
    }

    // ================================================================== PA_CRD_EVENT

    /// <summary>The stored answer of an order-sign event (table PA_CRD_EVENT).</summary>
    /// <remarks>The gateway calls this first at every order-sign: 404 means the event is new and the payer must be asked.</remarks>
    /// <param name="eventId" example="evt-20261007-0001">The EHR's id of the signing event, e.g. evt-20261007-0001.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No row with this event id (the event was not answered yet).</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("crd-events/{eventId}")]
    [ProducesResponseType(typeof(CrdEventRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetCrdEvent(string eventId)
    {
        return GetRow("crd-events", eventId);
    }

    /// <summary>Saves the answer of an order-sign event (table PA_CRD_EVENT).</summary>
    /// <remarks>Insert, or update when the event id already exists. Called once at the end of every order-sign.</remarks>
    /// <param name="eventId" example="evt-20261007-0001">The EHR's id of the signing event (key). Wins over eventId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("crd-events/{eventId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutCrdEvent(string eventId, [FromBody, Required] CrdEventRow row)
    {
        row.EventId = eventId;
        return PutRow("crd-events", eventId, row);
    }

    // ================================================================== PA_COVERAGE_DECISION

    /// <summary>The latest coverage decision of an order (table PA_COVERAGE_DECISION).</summary>
    /// <param name="orderId" example="ord-1-hd">EHR order id, e.g. ord-1-hd.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No decision is stored for this order.</response>
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
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("coverage-decisions/{orderId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

    // ================================================================== PA_DTR_FORM

    /// <summary>One DTR form: the payer questionnaire and its answers (table PA_DTR_FORM).</summary>
    /// <param name="formId" example="frm-235b7ad69e85">Form id created by the gateway, e.g. frm-235b7ad69e85.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No form with this id.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("dtr-forms/{formId}")]
    [ProducesResponseType(typeof(DtrFormRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetDtrForm(string formId)
    {
        return GetRow("dtr-forms", formId);
    }

    /// <summary>Saves a DTR form (table PA_DTR_FORM).</summary>
    /// <remarks>Insert, or update when the form id exists. Called when the questionnaire is fetched and again each time answers are saved.</remarks>
    /// <param name="formId" example="frm-235b7ad69e85">Form id (key). Wins over formId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("dtr-forms/{formId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutDtrForm(string formId, [FromBody, Required] DtrFormRow row)
    {
        row.FormId = formId;
        return PutRow("dtr-forms", formId, row);
    }

    // ================================================================== PA_DTR_LINK

    /// <summary>One payer SMART DTR app link (table PA_DTR_LINK).</summary>
    /// <param name="linkId" example="514ff06354c7485a907aec22535743f2">Link id created by the gateway.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No link with this id.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("dtr-links/{linkId}")]
    [ProducesResponseType(typeof(DtrLinkRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetDtrLink(string linkId)
    {
        return GetRow("dtr-links", linkId);
    }

    /// <summary>Saves a payer SMART DTR app link (table PA_DTR_LINK).</summary>
    /// <remarks>Insert, or update when the link id exists. Called at order-sign when the payer's answer has a DTR app link.</remarks>
    /// <param name="linkId" example="514ff06354c7485a907aec22535743f2">Link id (key). Wins over linkId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("dtr-links/{linkId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutDtrLink(string linkId, [FromBody, Required] DtrLinkRow row)
    {
        row.LinkId = linkId;
        return PutRow("dtr-links", linkId, row);
    }

    // ================================================================== PA_PRIOR_AUTH

    /// <summary>One prior authorization request (table PA_PRIOR_AUTH).</summary>
    /// <param name="requestId" example="PA-2026-0001">The EHR's request id, e.g. PA-2026-0001.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No request with this id.</response>
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
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("prior-auths/{requestId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutPriorAuth(string requestId, [FromBody, Required] PriorAuthRow row)
    {
        row.RequestId = requestId;
        lock (_store.Sync)
        {
            Table("prior-auths")[requestId] = ToJson(row);
            ShowPriorAuthDecision(requestId, row.DecisionJson);
            _store.Save();
        }
        return NoContent();
    }

    /// <summary>Prior authorization requests with a given status (table PA_PRIOR_AUTH).</summary>
    /// <remarks>
    /// The gateway's background job asks for status=pended every few minutes to find the requests still waiting for a payer.
    /// SQL: WHERE STATUS = :status [AND UPPER(PAYER_NAME) = UPPER(:payerName)].
    /// </remarks>
    /// <param name="status">Status to return, e.g. pended. Empty = every status.</param>
    /// <param name="payerName">Optional: only the requests of this payer (upper / lower case does not matter).</param>
    /// <response code="200">The matching rows as a JSON array ([] when nothing matches).</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("prior-auths")]
    [ProducesResponseType(typeof(List<PriorAuthRow>), StatusCodes.Status200OK)]
    public IActionResult ListPriorAuths([FromQuery] string? status, [FromQuery] string? payerName)
    {
        List<JsonElement> result = new List<JsonElement>();
        lock (_store.Sync)
        {
            foreach (JsonElement row in Table("prior-auths").Values)
            {
                bool statusMatches = status == null || Text(row, "status") == status;
                bool payerMatches = payerName == null || string.Equals(Text(row, "payerName"), payerName, StringComparison.OrdinalIgnoreCase);
                if (statusMatches && payerMatches)
                {
                    result.Add(row);
                }
            }
        }
        return Ok(result);
    }

    // ================================================================== PA_PAYER_SUBSCRIPTION

    /// <summary>The PAS Subscription of a payer + organization NPI (table PA_PAYER_SUBSCRIPTION).</summary>
    /// <remarks>The gateway asks this after a submit: 404 means it must still subscribe at the payer for this organization.</remarks>
    /// <param name="payerName" example="MockPayer">Payer name (upper / lower case does not matter), e.g. MockPayer.</param>
    /// <param name="organizationNpi" example="1999999992">NPI of the provider organization, e.g. 1999999992.</param>
    /// <response code="200">The row.</response>
    /// <response code="404">No subscription is stored for this payer and organization.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpGet("payer-subscriptions/{payerName}/{organizationNpi}")]
    [ProducesResponseType(typeof(PayerSubscriptionRow), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetPayerSubscription(string payerName, string organizationNpi)
    {
        return GetRow("payer-subscriptions", payerName.ToLowerInvariant() + "|" + organizationNpi);
    }

    /// <summary>Saves the PAS Subscription of a payer + organization NPI (table PA_PAYER_SUBSCRIPTION).</summary>
    /// <remarks>Insert, or update when the row exists. Called once, the first time a request is submitted for the organization.</remarks>
    /// <param name="payerName" example="MockPayer">Payer name (key, part 1). Wins over payerName in the body.</param>
    /// <param name="organizationNpi" example="1999999992">Organization NPI (key, part 2). Wins over organizationNpi in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("payer-subscriptions/{payerName}/{organizationNpi}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutPayerSubscription(string payerName, string organizationNpi, [FromBody, Required] PayerSubscriptionRow row)
    {
        row.PayerName = payerName;
        row.OrganizationNpi = organizationNpi;
        return PutRow("payer-subscriptions", payerName.ToLowerInvariant() + "|" + organizationNpi, row);
    }

    // ================================================================== PA_CDEX_TASK

    /// <summary>Saves a payer information request, a FHIR Task (table PA_CDEX_TASK).</summary>
    /// <remarks>Insert, or update when the Task exists. Called when the payer asks for more documents and each time the Task changes.</remarks>
    /// <param name="payerName" example="MockPayer">Payer name (key, part 1). Wins over payerName in the body.</param>
    /// <param name="taskId" example="5f367f122eba45caba287738b1cc2a38">The payer's Task id (key, part 2). Wins over taskId in the body.</param>
    /// <param name="row">Required. The row to save.</param>
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.GatewayData)]
    [HttpPut("cdex-tasks/{payerName}/{taskId}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    public IActionResult PutCdexTask(string payerName, string taskId, [FromBody, Required] CdexTaskRow row)
    {
        row.PayerName = payerName;
        row.TaskId = taskId;
        return PutRow("cdex-tasks", payerName.ToLowerInvariant() + "|" + taskId, row);
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
    /// <response code="404">No form with this id.</response>
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
    /// <response code="204">Saved.</response>
    /// <response code="400">The body is missing or is not valid JSON.</response>
    [Tags(SwaggerGroups.FhirApiData)]
    [HttpPut("questionnaire-responses/{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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

    /// <summary>One row, or 404.</summary>
    private IActionResult GetRow(string table, string key)
    {
        lock (_store.Sync)
        {
            JsonElement row;
            if (Table(table).TryGetValue(key, out row))
            {
                return Ok(row);
            }
        }
        return NotFound(new ErrorResponse("No row " + key + " in " + table + "."));
    }

    /// <summary>Inserts or replaces one row.</summary>
    private IActionResult PutRow(string table, string key, object row)
    {
        lock (_store.Sync)
        {
            Table(table)[key] = ToJson(row);
            _store.Save();
        }
        return NoContent();
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
        return Ok(result);
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
