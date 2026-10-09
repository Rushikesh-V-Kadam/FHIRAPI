namespace FHIRAPI.Constants;

/// <summary>
/// OperationOutcome.issue.code values used in error responses.
/// </summary>
public static class IssueTypes
{
    public const string Invalid = "invalid";
    public const string Required = "required";
    public const string NotFound = "not-found";
    public const string NotSupported = "not-supported";
    public const string Security = "security";
    public const string Forbidden = "forbidden";
    public const string Exception = "exception";
}
