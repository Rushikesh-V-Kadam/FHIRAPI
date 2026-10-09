namespace MockEhr.Api.Models;

/// <summary>
/// An encounter (visit), as the EHR returns it from GET api/encounters/{encounterId} and
/// GET api/patients/{patientId}/encounters. Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Encounter.
/// </summary>
public class EhrEncounterDto
{
    /// <summary>EHR encounter id.</summary>
    /// <example>enc-1</example>
    public string EncounterId { get; set; } = string.Empty;

    /// <summary>Patient of the encounter.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>planned | arrived | in-progress | finished (or completed) | cancelled.</summary>
    /// <example>in-progress</example>
    public string Status { get; set; } = "finished";

    /// <summary>inpatient | emergency | home | virtual; anything else (e.g. AMB, outpatient) = ambulatory.</summary>
    /// <example>AMB</example>
    public string? EncounterClass { get; set; }

    /// <summary>Type of visit as a code.</summary>
    /// <example>99214</example>
    public string? TypeCode { get; set; }

    /// <summary>Code system of typeCode: CPT, HCPCS or SNOMED.</summary>
    /// <example>CPT</example>
    public string? TypeCodeSystem { get; set; }

    /// <summary>Text of the visit type.</summary>
    /// <example>Nephrology office visit</example>
    public string? TypeDisplay { get; set; }

    /// <summary>Start (UTC, ISO-8601 with Z). The from filter of the list compares with this value.</summary>
    /// <example>2026-09-28T09:30:00Z</example>
    public string? StartDateTime { get; set; }

    /// <summary>End (UTC, ISO-8601 with Z). Empty while the encounter is open.</summary>
    /// <example>2026-09-28T10:15:00Z</example>
    public string? EndDateTime { get; set; }

    /// <summary>Attending clinician.</summary>
    /// <example>prac-1</example>
    public string? PractitionerId { get; set; }

    /// <summary>Organization responsible for the encounter.</summary>
    /// <example>org-1</example>
    public string? OrganizationId { get; set; }

    /// <summary>Where the encounter takes place.</summary>
    /// <example>loc-office</example>
    public string? LocationId { get; set; }

    /// <summary>Diagnoses that are the reason for the encounter (codeSystem ICD10CM).</summary>
    public List<EhrCode> ReasonDiagnoses { get; set; } = new();

    /// <summary>Discharge disposition code (inpatient), e.g. home, snf, hosp.</summary>
    /// <example>home</example>
    public string? DischargeDisposition { get; set; }
}
