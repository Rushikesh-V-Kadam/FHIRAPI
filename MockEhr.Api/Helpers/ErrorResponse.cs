namespace MockEhr.Api.Helpers;

/// <summary>Error body of the EHR APIs: { "error": "..." }. The gateway and the FHIR API log the first 300 characters.</summary>
public class ErrorResponse
{
    /// <summary>What went wrong, in plain text.</summary>
    /// <example>Patient pat-9 not found.</example>
    public string Error { get; set; } = string.Empty;

    /// <summary>Creates an error.</summary>
    public ErrorResponse(string error)
    {
        Error = error;
    }
}
