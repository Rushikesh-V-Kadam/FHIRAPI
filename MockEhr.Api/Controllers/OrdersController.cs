using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>
/// Orders (service, device, medication) and the CRD coverage decision the Payer Gateway writes back for each order.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class OrdersController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public OrdersController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>Orders, optionally of one patient.</summary>
    /// <param name="patientId">Only the orders of this patient, e.g. pat-1. Empty = all orders.</param>
    /// <response code="200">The orders.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet]
    [ProducesResponseType(typeof(EhrList<EhrOrderDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrOrderDto>> List([FromQuery] string? patientId)
    {
        List<EhrOrderDto> result = new List<EhrOrderDto>();
        lock (_store.Sync)
        {
            foreach (EhrOrderDto order in _store.Data.Orders)
            {
                if (patientId == null || order.PatientId == patientId)
                {
                    result.Add(order);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>One order.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR ServiceRequest, DeviceRequest or MedicationRequest, depending on orderType
    /// (GET /fhir/r4/ServiceRequest/{id} ...). This is the order the payer checks at order-sign.
    /// </remarks>
    /// <param name="id" example="ord-1-hd">EHR order id, e.g. ord-1-hd.</param>
    /// <response code="200">The order.</response>
    /// <response code="404">No order with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrOrderDto> Get(string id)
    {
        lock (_store.Sync)
        {
            EhrOrderDto? order = FindOrder(id);
            if (order == null)
            {
                return NotFound(new ErrorResponse("Order " + id + " not found."));
            }
            return Ok(order);
        }
    }

    /// <summary>Adds an order (status draft until it is signed).</summary>
    /// <remarks>
    /// patientId, code, codeSystem and orderType must be filled. An empty orderId gets a new id.
    /// A renewal (requestType = renewal) also needs previousAuthorizationNumber.
    /// </remarks>
    /// <param name="order">Required. The order to add.</param>
    /// <response code="201">The order as stored (with its id).</response>
    /// <response code="400">A required field is missing or has a wrong value, or the patient does not exist.</response>
    /// <response code="409">An order with this id already exists.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public ActionResult<EhrOrderDto> Create([FromBody, Required] EhrOrderDto order)
    {
        string? problem = CheckOrder(order);
        if (problem != null)
        {
            return BadRequest(new ErrorResponse(problem));
        }
        lock (_store.Sync)
        {
            if (!PatientExists(order.PatientId))
            {
                return BadRequest(new ErrorResponse("Patient " + order.PatientId + " not found."));
            }
            if (string.IsNullOrWhiteSpace(order.OrderId))
            {
                order.OrderId = EhrStore.NewId("ord");
            }
            if (FindOrder(order.OrderId) != null)
            {
                return Conflict(new ErrorResponse("Order " + order.OrderId + " already exists."));
            }
            if (string.IsNullOrWhiteSpace(order.Status))
            {
                order.Status = "draft";
            }
            if (string.IsNullOrWhiteSpace(order.OrderedDateTime))
            {
                order.OrderedDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            }
            _store.Data.Orders.Add(order);
            _store.Save();
        }
        return Created("/api/orders/" + order.OrderId, order);
    }

    /// <summary>Changes an order (for example status "active" after signing).</summary>
    /// <remarks>
    /// The same fields must be filled as when adding. The id in the URL wins over orderId in the body.
    /// </remarks>
    /// <param name="id" example="ord-1-hd">EHR order id, e.g. ord-1-hd.</param>
    /// <param name="order">Required. The whole order (replaces the stored one).</param>
    /// <response code="200">The order as stored.</response>
    /// <response code="400">A required field is missing or has a wrong value.</response>
    /// <response code="404">No order with this id.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrOrderDto> Update(string id, [FromBody, Required] EhrOrderDto order)
    {
        string? problem = CheckOrder(order);
        if (problem != null)
        {
            return BadRequest(new ErrorResponse(problem));
        }
        lock (_store.Sync)
        {
            for (int i = 0; i < _store.Data.Orders.Count; i++)
            {
                if (_store.Data.Orders[i].OrderId == id)
                {
                    order.OrderId = id;
                    _store.Data.Orders[i] = order;
                    _store.Save();
                    return Ok(order);
                }
            }
        }
        return NotFound(new ErrorResponse("Order " + id + " not found."));
    }

    // ================================================================== CRD write-back

    /// <summary>Latest coverage decision of an order, as the order screen shows it.</summary>
    /// <remarks>
    /// Read from the decisionJson column of table PA_COVERAGE_DECISION (saved by the Payer Gateway through the data API).
    /// </remarks>
    /// <param name="id" example="ord-1-hd">EHR order id, e.g. ord-1-hd.</param>
    /// <response code="200">The decision.</response>
    /// <response code="404">No decision is stored for this order.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet("{id}/coverage-decision")]
    [ProducesResponseType(typeof(CoverageDecisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<CoverageDecisionDto> GetCoverageDecision(string id)
    {
        lock (_store.Sync)
        {
            foreach (CoverageDecisionDto decision in _store.Data.CoverageDecisions)
            {
                if (decision.OrderId == id)
                {
                    return Ok(decision);
                }
            }
        }
        return NotFound(new ErrorResponse("No coverage decision for order " + id + "."));
    }

    /// <summary>Old write-back: the Payer Gateway used to write the CRD decision of an order here.</summary>
    /// <remarks>
    /// Not called any more: the decision is saved with PUT api/prior-auth-data/coverage-decisions/{orderId}. The real EHR does not need this endpoint.
    /// </remarks>
    /// <param name="id" example="ord-1-hd">EHR order id, e.g. ord-1-hd.</param>
    /// <param name="decision">Required. The decision.</param>
    /// <response code="204">Saved.</response>
    /// <response code="404">No order with this id.</response>
    [Tags(SwaggerGroups.Unused)]
    [HttpPut("{id}/coverage-decision")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult PutCoverageDecision(string id, [FromBody, Required] CoverageDecisionDto decision)
    {
        lock (_store.Sync)
        {
            if (FindOrder(id) == null)
            {
                return NotFound(new ErrorResponse("Order " + id + " not found."));
            }
            decision.OrderId = id;
            bool replaced = false;
            for (int i = 0; i < _store.Data.CoverageDecisions.Count; i++)
            {
                if (_store.Data.CoverageDecisions[i].OrderId == id)
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
            _store.LogWriteBack("coverage-decision", id, decision);
            _store.Save();
        }
        return NoContent();
    }

    // ================================================================== helpers

    /// <summary>The order with this id, or null (call inside the lock).</summary>
    private EhrOrderDto? FindOrder(string id)
    {
        foreach (EhrOrderDto order in _store.Data.Orders)
        {
            if (order.OrderId == id)
            {
                return order;
            }
        }
        return null;
    }

    /// <summary>True when the patient exists (call inside the lock).</summary>
    private bool PatientExists(string patientId)
    {
        foreach (EhrPatientDto patient in _store.Data.Patients)
        {
            if (patient.PatientId == patientId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Required order fields (fills the default request and patient type); null when fine.</summary>
    private static string? CheckOrder(EhrOrderDto order)
    {
        if (string.IsNullOrWhiteSpace(order.PatientId))
        {
            return "patientId is required.";
        }
        if (string.IsNullOrWhiteSpace(order.Code) || string.IsNullOrWhiteSpace(order.CodeSystem))
        {
            return "code and codeSystem (CPT, HCPCS, RXNORM) are required.";
        }
        if (order.OrderType != "service" && order.OrderType != "device" && order.OrderType != "medication")
        {
            return "orderType must be service, device or medication.";
        }
        if (string.IsNullOrWhiteSpace(order.RequestType))
        {
            order.RequestType = "new";          // empty = new authorization
        }
        if (string.IsNullOrWhiteSpace(order.PatientType))
        {
            order.PatientType = "permanent";    // empty = permanent patient
        }
        if (order.RequestType != "new" && order.RequestType != "renewal")
        {
            return "requestType must be new or renewal.";
        }
        if (order.PatientType != "permanent" && order.PatientType != "transient")
        {
            return "patientType must be permanent or transient.";
        }
        if (order.RequestType == "renewal" && string.IsNullOrWhiteSpace(order.PreviousAuthorizationNumber))
        {
            return "previousAuthorizationNumber is required for a renewal.";
        }
        return null;
    }
}
