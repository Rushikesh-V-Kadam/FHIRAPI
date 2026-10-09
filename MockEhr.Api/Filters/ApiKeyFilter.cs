using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MockEhr.Api.Helpers;

namespace MockEhr.Api.Filters;

/// <summary>
/// Optional X-Api-Key check (MockEhr:ApiKey in appsettings; must equal Ehr:ApiKey in the gateway and the FHIR API).
/// Empty key = no check. Applied to every /api controller with [ServiceFilter(typeof(ApiKeyFilter))].
/// </summary>
public class ApiKeyFilter : IActionFilter
{
    private readonly string? _apiKey;

    /// <summary>Created by dependency injection.</summary>
    public ApiKeyFilter(IConfiguration configuration)
    {
        _apiKey = configuration["MockEhr:ApiKey"];
    }

    /// <summary>Returns 401 when a key is configured and the request does not send it.</summary>
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return;
        }
        string sent = context.HttpContext.Request.Headers["X-Api-Key"].ToString();
        if (sent != _apiKey)
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse("Missing or invalid X-Api-Key."));
        }
    }

    /// <summary>Nothing to do after the action.</summary>
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
