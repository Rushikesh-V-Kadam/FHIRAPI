using Microsoft.AspNetCore.Diagnostics;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Middleware;

/// <summary>
/// Turns every unhandled exception into a FHIR OperationOutcome, so controllers need no try/catch:
///  - FhirException -> its own status (400 / 403 / 404)
///  - EhrException  -> 502 (the EHR failed)
///  - anything else -> 500
/// </summary>
public class FhirExceptionHandler : IExceptionHandler
{
    private readonly ILogger<FhirExceptionHandler> _logger;

    /// <summary>Created by dependency injection.</summary>
    public FhirExceptionHandler(ILogger<FhirExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>Writes the OperationOutcome. Returns true = handled.</summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        int status;
        string code;
        string message;

        if (exception is FhirException fhirError)
        {
            status = fhirError.StatusCode;
            code = fhirError.IssueCode;
            message = fhirError.Message;
        }
        else if (exception is EhrException)
        {
            status = StatusCodes.Status502BadGateway;
            code = IssueTypes.Exception;
            message = $"EHR API error: {exception.Message}";
            _logger.LogWarning(exception, "EHR call failed");
        }
        else
        {
            status = StatusCodes.Status500InternalServerError;
            code = IssueTypes.Exception;
            message = $"Unexpected error (correlation id {CorrelationContext.Id}).";
            _logger.LogError(exception, "Unhandled error");
        }

        var severity = status >= 500 ? "fatal" : "error";
        context.Response.StatusCode = status;
        context.Response.ContentType = FhirMediaTypes.FhirJson;
        await context.Response.WriteAsync(FhirJson.Serialize(OperationOutcome.Create(severity, code, message)), cancellationToken);
        return true;
    }
}
