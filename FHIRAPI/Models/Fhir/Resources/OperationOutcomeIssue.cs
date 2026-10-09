namespace FHIRAPI.Models.Fhir;

/// <summary>
/// One problem inside an OperationOutcome.
/// </summary>
public class OperationOutcomeIssue
{
    public string Severity { get; set; } = "error";
    public string Code { get; set; } = "processing";
    public string? Diagnostics { get; set; }
}
