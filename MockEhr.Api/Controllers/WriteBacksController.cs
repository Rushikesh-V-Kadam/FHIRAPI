using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Test helpers: what the gateway wrote to the EHR, and a reset to the seed data.</summary>
[ApiController]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class WriteBacksController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public WriteBacksController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>The last 200 things the gateway and the FHIR API saved, newest first.</summary>
    /// <remarks>
    /// A log for the Mock EHR UI: coverage decisions, prior authorization decisions and DTR forms, with the saved body.
    /// </remarks>
    /// <response code="200">The log entries.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet("api/write-backs")]
    [ProducesResponseType(typeof(List<WriteBack>), StatusCodes.Status200OK)]
    public ActionResult<List<WriteBack>> List()
    {
        List<WriteBack> newestFirst = new List<WriteBack>();
        lock (_store.Sync)
        {
            for (int i = _store.Data.WriteBacks.Count - 1; i >= 0; i--)
            {
                newestFirst.Add(_store.Data.WriteBacks[i]);
            }
        }
        return Ok(newestFirst);
    }

    /// <summary>Deletes all data (also what the EHR app added) and loads the seed data again.</summary>
    /// <response code="200">Done: { "status": "reset", "file": "path of the data file" }.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost("api/admin/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Reset()
    {
        _store.Reset();
        return Ok(new Dictionary<string, string> { { "status", "reset" }, { "file", _store.GetFilePath() } });
    }

    /// <summary>Opens Swagger UI when the root URL is browsed.</summary>
    [HttpGet("/")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult Root()
    {
        return Redirect("/swagger");
    }
}
