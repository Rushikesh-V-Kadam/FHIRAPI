using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Service locations (with CMS place-of-service code).</summary>
[ApiController]
[Route("api/locations")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class LocationsController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public LocationsController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>All locations.</summary>
    /// <response code="200">The locations.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet]
    [ProducesResponseType(typeof(EhrList<EhrLocationDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrLocationDto>> List()
    {
        lock (_store.Sync)
        {
            return Ok(ListResult.Of(new List<EhrLocationDto>(_store.Data.Locations)));
        }
    }

    /// <summary>One location.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Location (GET /fhir/r4/Location/{id}).
    /// </remarks>
    /// <param name="id" example="loc-main">EHR location id, e.g. loc-main.</param>
    /// <response code="200">The location.</response>
    /// <response code="404">No location with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrLocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrLocationDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrLocationDto location in _store.Data.Locations)
            {
                if (location.LocationId == id)
                {
                    return Ok(location);
                }
            }
        }
        return NotFound(new ErrorResponse("Location " + id + " not found."));
    }
}
