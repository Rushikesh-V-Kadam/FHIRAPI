namespace FHIRAPI.Constants;

/// <summary>
/// Custom HTTP header names.
/// </summary>
public static class ApiHeaders
{
    /// <summary>Id that ties together the log lines of one request across EHR, FHIR API and Payer Gateway.</summary>
    public const string CorrelationId = "X-Correlation-Id";
}
