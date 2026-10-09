using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Appointments (read by the FHIR API).</summary>
[ApiController]
[Route("api/appointments")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class AppointmentsController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public AppointmentsController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>One appointment.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Appointment (GET /fhir/r4/Appointment/{id}).
    /// </remarks>
    /// <param name="id" example="appt-1">EHR appointment id, e.g. appt-1.</param>
    /// <response code="200">The appointment.</response>
    /// <response code="404">No appointment with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrAppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrAppointmentDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrAppointmentDto appointment in _store.Data.Appointments)
            {
                if (appointment.AppointmentId == id)
                {
                    return Ok(appointment);
                }
            }
        }
        return NotFound(new ErrorResponse("Appointment " + id + " not found."));
    }
}
