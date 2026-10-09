using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR OperationOutcome: the error body returned by every /fhir/r4 endpoint when something fails.
/// </summary>
public class OperationOutcome : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "OperationOutcome";

    public List<OperationOutcomeIssue> Issue { get; set; } = new();

    /// <summary>Creates an outcome with one issue.</summary>
    /// <param name="severity">fatal | error | warning | information</param>
    /// <param name="code">Issue type, see Constants/IssueTypes.</param>
    /// <param name="diagnostics">Human-readable message.</param>
    public static OperationOutcome Create(string severity, string code, string diagnostics)
    {
        var outcome = new OperationOutcome();
        outcome.Issue.Add(new OperationOutcomeIssue { Severity = severity, Code = code, Diagnostics = diagnostics });
        return outcome;
    }
}
