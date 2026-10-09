namespace MockEhr.Api.Models;

/// <summary>
/// A lab result or vital sign, as the EHR returns it from GET api/patients/{patientId}/observations.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Observation; payers and DTR forms read
/// these values (eGFR, creatinine, Kt/V, hemoglobin ...) to fill in questionnaires automatically.
/// Fill exactly one of valueNumber (+ unit), valueText or valueCode.
/// </summary>
public class EhrObservationDto
{
    /// <summary>EHR observation id.</summary>
    /// <example>obs-1-egfr</example>
    public string ObservationId { get; set; } = string.Empty;

    /// <summary>Patient of the result.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Encounter in which it was measured.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>laboratory | vital-signs ... Empty = laboratory. The category filter of the list compares with this value.</summary>
    /// <example>laboratory</example>
    public string? Category { get; set; }

    /// <summary>What was measured, normally a LOINC code. The codes filter of the list compares with this value.</summary>
    /// <example>33914-3</example>
    public string Code { get; set; } = string.Empty;

    /// <summary>Code system of the code.</summary>
    /// <example>LOINC</example>
    public string CodeSystem { get; set; } = "LOINC";

    /// <summary>Text of the code.</summary>
    /// <example>eGFR (MDRD)</example>
    public string? Display { get; set; }

    /// <summary>Numeric result.</summary>
    /// <example>8</example>
    public decimal? ValueNumber { get; set; }

    /// <summary>Unit of valueNumber (UCUM).</summary>
    /// <example>mL/min/1.73m2</example>
    public string? Unit { get; set; }

    /// <summary>Text result, when the result is not a number.</summary>
    /// <example>Negative</example>
    public string? ValueText { get; set; }

    /// <summary>Coded result, when the result is a code.</summary>
    public EhrCode? ValueCode { get; set; }

    /// <summary>FHIR observation status: registered | preliminary | final | amended | cancelled.</summary>
    /// <example>final</example>
    public string Status { get; set; } = "final";

    /// <summary>When the sample was taken / the value was measured (UTC, ISO-8601 with Z). The from filter of the list compares with this value.</summary>
    /// <example>2026-09-22T07:00:00Z</example>
    public string? EffectiveDateTime { get; set; }

    /// <summary>When the result was reported (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-09-22T07:00:00Z</example>
    public string? IssuedDateTime { get; set; }
}
