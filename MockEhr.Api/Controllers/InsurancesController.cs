using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>One insurance (coverage) by its coverage id, unique in the EHR.</summary>
[ApiController]
[Route("api/insurances")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class InsurancesController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public InsurancesController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>One insurance by its coverage id.</summary>
    /// <remarks>
    /// The EHR sends this coverage id to the Payer Gateway (coverageId in order-sign, DTR and PAS); the FHIR API reads the
    /// insurance with it and turns it into a FHIR Coverage (GET /fhir/r4/Coverage/{coverageId}). The coverage id is unique
    /// in the EHR, so exactly one insurance comes back. The insurance names its patient (patientId).
    /// </remarks>
    /// <param name="coverageId" example="cov-1">EHR coverage id, e.g. cov-1.</param>
    /// <response code="200">The insurance.</response>
    /// <response code="404">No insurance with this coverage id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{coverageId}")]
    [ProducesResponseType(typeof(EhrInsuranceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrInsuranceDto> Get(string coverageId)
    {
        lock (_store.Sync)
        {
            foreach (EhrInsuranceDto insurance in _store.Data.Insurances)
            {
                if (insurance.CoverageId == coverageId)
                {
                    return Ok(insurance);
                }
            }
        }
        return NotFound(new ErrorResponse("Insurance " + coverageId + " not found."));
    }
}
