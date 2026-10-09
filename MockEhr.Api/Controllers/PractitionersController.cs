using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Practitioners (clinicians). GET {id} is read by the FHIR API; the list and POST are for the EHR app.</summary>
[ApiController]
[Route("api/practitioners")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class PractitionersController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public PractitionersController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>All practitioners.</summary>
    /// <response code="200">The practitioners.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet]
    [ProducesResponseType(typeof(EhrList<EhrPractitionerDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrPractitionerDto>> List()
    {
        lock (_store.Sync)
        {
            return Ok(ListResult.Of(new List<EhrPractitionerDto>(_store.Data.Practitioners)));
        }
    }

    /// <summary>One practitioner.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Practitioner and PractitionerRole (GET /fhir/r4/Practitioner/{id}). The NPI is needed for prior authorization requests.
    /// </remarks>
    /// <param name="id" example="prac-1">EHR practitioner id, e.g. prac-1.</param>
    /// <response code="200">The practitioner.</response>
    /// <response code="404">No practitioner with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrPractitionerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrPractitionerDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrPractitionerDto practitioner in _store.Data.Practitioners)
            {
                if (practitioner.PractitionerId == id)
                {
                    return Ok(practitioner);
                }
            }
        }
        return NotFound(new ErrorResponse("Practitioner " + id + " not found."));
    }

    /// <summary>Adds a practitioner.</summary>
    /// <remarks>
    /// lastName and npi must be filled (PAS needs the NPI). An empty practitionerId gets a new id.
    /// </remarks>
    /// <param name="practitioner">Required. The practitioner to add.</param>
    /// <response code="201">The practitioner as stored (with its id).</response>
    /// <response code="400">lastName or npi is missing.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrPractitionerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<EhrPractitionerDto> Create([FromBody, Required] EhrPractitionerDto practitioner)
    {
        if (string.IsNullOrWhiteSpace(practitioner.LastName) || string.IsNullOrWhiteSpace(practitioner.Npi))
        {
            return BadRequest(new ErrorResponse("lastName and npi are required."));
        }
        lock (_store.Sync)
        {
            if (string.IsNullOrWhiteSpace(practitioner.PractitionerId))
            {
                practitioner.PractitionerId = EhrStore.NewId("prac");
            }
            if (practitioner.Active == null)
            {
                practitioner.Active = true;
            }
            _store.Data.Practitioners.Add(practitioner);
            _store.Save();
        }
        return Created("/api/practitioners/" + practitioner.PractitionerId, practitioner);
    }
}
