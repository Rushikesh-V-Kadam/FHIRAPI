namespace FHIRAPI.Exceptions;

/// <summary>
/// A request problem the caller must fix (bad search, missing parameter, not found, forbidden).
/// FhirExceptionHandler turns it into an OperationOutcome with the given HTTP status.
/// </summary>
public class FhirException : Exception
{
    /// <summary>HTTP status to return, e.g. 400, 403, 404.</summary>
    public int StatusCode { get; }

    /// <summary>OperationOutcome issue code, see Constants/IssueTypes.</summary>
    public string IssueCode { get; }

    /// <summary>Creates the exception.</summary>
    public FhirException(int statusCode, string issueCode, string message) : base(message)
    {
        StatusCode = statusCode;
        IssueCode = issueCode;
    }
}
