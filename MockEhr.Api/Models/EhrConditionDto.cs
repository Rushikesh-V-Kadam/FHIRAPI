namespace MockEhr.Api.Models;

/// <summary>
/// A diagnosis of a patient, as the EHR returns it from GET api/patients/{patientId}/conditions.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Condition.
/// </summary>
public class EhrConditionDto
{
    /// <summary>EHR condition id.</summary>
    /// <example>cond-1-esrd</example>
    public string ConditionId { get; set; } = string.Empty;

    /// <summary>Patient who has the condition.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Diagnosis code. The codes filter of the list compares with this value.</summary>
    /// <example>N18.6</example>
    public string Code { get; set; } = string.Empty;

    /// <summary>Code system of the code: ICD10CM or SNOMED.</summary>
    /// <example>ICD10CM</example>
    public string CodeSystem { get; set; } = "ICD10CM";

    /// <summary>Text of the code.</summary>
    /// <example>End stage renal disease</example>
    public string? Display { get; set; }

    /// <summary>active | recurrence | relapse | inactive | remission | resolved. The status filter of the list compares with this value.</summary>
    /// <example>active</example>
    public string? ClinicalStatus { get; set; }

    /// <summary>unconfirmed | provisional | differential | confirmed | refuted | entered-in-error.</summary>
    /// <example>confirmed</example>
    public string? VerificationStatus { get; set; }

    /// <summary>problem-list-item | encounter-diagnosis. Empty = problem-list-item.</summary>
    /// <example>problem-list-item</example>
    public string? Category { get; set; }

    /// <summary>When the condition started, yyyy-MM-dd.</summary>
    /// <example>2024-02-10</example>
    public string? OnsetDate { get; set; }

    /// <summary>When it was recorded, yyyy-MM-dd.</summary>
    /// <example>2024-02-12</example>
    public string? RecordedDate { get; set; }

    /// <summary>Encounter in which it was recorded.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }
}
