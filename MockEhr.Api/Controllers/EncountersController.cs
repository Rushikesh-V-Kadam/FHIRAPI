using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Encounters (read by the FHIR API).</summary>
[ApiController]
[Route("api/encounters")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class EncountersController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public EncountersController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>One encounter.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Encounter (GET /fhir/r4/Encounter/{id}).
    /// </remarks>
    /// <param name="id" example="enc-1">EHR encounter id, e.g. enc-1.</param>
    /// <response code="200">The encounter.</response>
    /// <response code="404">No encounter with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrEncounterDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrEncounterDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrEncounterDto encounter in _store.Data.Encounters)
            {
                if (encounter.EncounterId == id)
                {
                    return Ok(encounter);
                }
            }
        }
        return NotFound(new ErrorResponse("Encounter " + id + " not found."));
    }
}
