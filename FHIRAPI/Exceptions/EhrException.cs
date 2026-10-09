namespace FHIRAPI.Exceptions;

/// <summary>
/// The EHR REST API failed or could not be reached. Returned to the caller as 502 Bad Gateway.
/// </summary>
public class EhrException : Exception
{
    /// <summary>Creates the exception.</summary>
    public EhrException(string message, Exception? inner = null) : base(message, inner) { }
}
