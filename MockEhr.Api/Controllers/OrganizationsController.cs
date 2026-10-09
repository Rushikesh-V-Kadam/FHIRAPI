using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Provider organizations. GET {id} is read by the FHIR API; the list and POST are for the EHR app.</summary>
[ApiController]
[Route("api/organizations")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class OrganizationsController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public OrganizationsController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>All organizations.</summary>
    /// <response code="200">The organizations.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet]
    [ProducesResponseType(typeof(EhrList<EhrOrganizationDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrOrganizationDto>> List()
    {
        lock (_store.Sync)
        {
            return Ok(ListResult.Of(new List<EhrOrganizationDto>(_store.Data.Organizations)));
        }
    }

    /// <summary>One organization.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Organization (GET /fhir/r4/Organization/{id}). The NPI is needed for prior authorization requests.
    /// </remarks>
    /// <param name="id" example="org-1">EHR organization id, e.g. org-1.</param>
    /// <response code="200">The organization.</response>
    /// <response code="404">No organization with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrOrganizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrOrganizationDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrOrganizationDto organization in _store.Data.Organizations)
            {
                if (organization.OrganizationId == id)
                {
                    return Ok(organization);
                }
            }
        }
        return NotFound(new ErrorResponse("Organization " + id + " not found."));
    }

    /// <summary>Adds an organization.</summary>
    /// <remarks>
    /// name and npi must be filled (PAS needs the NPI). An empty organizationId gets a new id.
    /// </remarks>
    /// <param name="organization">Required. The organization to add.</param>
    /// <response code="201">The organization as stored (with its id).</response>
    /// <response code="400">name or npi is missing.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrOrganizationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public ActionResult<EhrOrganizationDto> Create([FromBody, Required] EhrOrganizationDto organization)
    {
        if (string.IsNullOrWhiteSpace(organization.Name) || string.IsNullOrWhiteSpace(organization.Npi))
        {
            return BadRequest(new ErrorResponse("name and npi are required."));
        }
        lock (_store.Sync)
        {
            if (string.IsNullOrWhiteSpace(organization.OrganizationId))
            {
                organization.OrganizationId = EhrStore.NewId("org");
            }
            if (organization.Active == null)
            {
                organization.Active = true;
            }
            _store.Data.Organizations.Add(organization);
            _store.Save();
        }
        return Created("/api/organizations/" + organization.OrganizationId, organization);
    }
}
