using FHIRAPI.Constants;

namespace FHIRAPI.Middleware;

/// <summary>
/// Reads X-Correlation-Id from the incoming request (or creates one), stores it for logging
/// and returns it in the response header, so one flow can be traced across EHR, FHIR API and Payer Gateway logs.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Created once by ASP.NET Core.</summary>
    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Runs for every request.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[ApiHeaders.CorrelationId].ToString();
        CorrelationContext.Id = string.IsNullOrWhiteSpace(incoming) ? Guid.NewGuid().ToString("N") : incoming;
        context.Response.Headers[ApiHeaders.CorrelationId] = CorrelationContext.Id;
        await _next(context);
    }
}
