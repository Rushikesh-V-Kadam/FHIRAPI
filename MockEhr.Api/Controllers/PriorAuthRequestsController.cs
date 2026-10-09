using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Prior authorization decisions written back by the Payer Gateway (PAS / CDex).</summary>
[ApiController]
[Route("api/prior-auth-requests")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class PriorAuthRequestsController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public PriorAuthRequestsController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>Prior authorization decisions, as the prior authorization screen shows them.</summary>
    /// <remarks>
    /// Read from the decisionJson column of table PA_PRIOR_AUTH (saved by the Payer Gateway through the data API).
    /// </remarks>
    /// <param name="patientId">Only the requests of this patient, e.g. pat-1. Empty = all.</param>
    /// <param name="orderId">Only the requests that contain this order, e.g. ord-1-hd. Empty = all.</param>
    /// <response code="200">The decisions.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet]
    [ProducesResponseType(typeof(EhrList<PriorAuthDecisionDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<PriorAuthDecisionDto>> List([FromQuery] string? patientId, [FromQuery] string? orderId)
    {
        List<PriorAuthDecisionDto> result = new List<PriorAuthDecisionDto>();
        lock (_store.Sync)
        {
            foreach (PriorAuthDecisionDto decision in _store.Data.PriorAuthDecisions)
            {
                bool patientMatches = patientId == null || decision.PatientId == patientId;
                bool orderMatches = orderId == null || decision.OrderIds.Contains(orderId);
                if (patientMatches && orderMatches)
                {
                    result.Add(decision);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>One prior authorization decision.</summary>
    /// <param name="id" example="PA-2026-0001">The EHR's request id, e.g. PA-2026-0001.</param>
    /// <response code="200">The decision.</response>
    /// <response code="404">No decision is stored for this request.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PriorAuthDecisionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<PriorAuthDecisionDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (PriorAuthDecisionDto decision in _store.Data.PriorAuthDecisions)
            {
                if (decision.RequestId == id)
                {
                    return Ok(decision);
                }
            }
        }
        return NotFound(new ErrorResponse("No prior authorization decision " + id + "."));
    }

    /// <summary>Old write-back: the Payer Gateway used to write the latest decision of a request here.</summary>
    /// <remarks>
    /// Not called any more: the decision is saved with PUT api/prior-auth-data/prior-auths/{requestId}. The real EHR does not need this endpoint.
    /// </remarks>
    /// <param name="id" example="PA-2026-0001">The EHR's request id, e.g. PA-2026-0001.</param>
    /// <param name="decision">Required. The decision.</param>
    /// <response code="204">Saved.</response>
    [Tags(SwaggerGroups.Unused)]
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Put(string id, [FromBody, Required] PriorAuthDecisionDto decision)
    {
        lock (_store.Sync)
        {
            decision.RequestId = id;
            bool replaced = false;
            for (int i = 0; i < _store.Data.PriorAuthDecisions.Count; i++)
            {
                if (_store.Data.PriorAuthDecisions[i].RequestId == id)
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
            _store.LogWriteBack("prior-auth-decision", id, decision);
            _store.Save();
        }
        return NoContent();
    }
}
