using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One SMART launch of a payer DTR app = one row of table PA_SMART_LAUNCH. Saved and read by the FHIR API.
/// Body of PUT api/prior-auth-data/smart-launches/{launchId}; returned by GET of the same path. Lives 15 minutes.
/// </summary>
public class SmartLaunchRow
{
    /// <summary>Key. Random launch id sent to the app (the same value as in the URL). Column LAUNCH_ID, VARCHAR2(64).</summary>
    /// <example>6-jM9cmaBKMd4rFPAl-hQp_D</example>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>Payer whose app may use the launch; empty = any registered app. Column INSURANCE_PLAN_ID, NUMBER(10).</summary>
    /// <example>1001</example>
    public int? InsurancePlanId { get; set; }

    /// <summary>EHR patient id. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Encounter in context, if any. Column ENCOUNTER_ID, VARCHAR2(64).</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>Logged-in clinician. Column USER_PRACTITIONER_ID, VARCHAR2(64).</summary>
    /// <example>prac-1</example>
    public string? UserPractitionerId { get; set; }

    /// <summary>JSON text: references handed to the app as fhirContext. Column FHIR_CONTEXT_JSON, CLOB.</summary>
    /// <example>["ServiceRequest/ord-1-hd","Coverage/cov-1"]</example>
    public string? FhirContextJson { get; set; }

    /// <summary>appContext from the payer's CRD card (free text, returned unchanged). Column APP_CONTEXT, CLOB.</summary>
    /// <example>{"questionnaire":["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"]}</example>
    public string? AppContext { get; set; }

    /// <summary>UTC, ISO-8601 with Z. Column EXPIRES_AT, TIMESTAMP.</summary>
    /// <example>2026-10-31T10:15:00Z</example>
    [Required]
    public DateTime ExpiresAt { get; set; }
}
