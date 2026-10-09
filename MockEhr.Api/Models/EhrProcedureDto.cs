namespace MockEhr.Api.Models;

/// <summary>
/// A procedure that was done, as the EHR returns it from GET api/patients/{patientId}/procedures.
/// Plain JSON (camelCase), not FHIR. The FHIR API turns it into a FHIR Procedure.
/// </summary>
public class EhrProcedureDto
{
    /// <summary>EHR procedure id.</summary>
    /// <example>proc-1-avf</example>
    public string ProcedureId { get; set; } = string.Empty;

    /// <summary>Patient of the procedure.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Procedure code. The codes filter of the list compares with this value.</summary>
    /// <example>36821</example>
    public string Code { get; set; } = string.Empty;

    /// <summary>Code system of the code: CPT, HCPCS or SNOMED.</summary>
    /// <example>CPT</example>
    public string CodeSystem { get; set; } = "CPT";

    /// <summary>Text of the code.</summary>
    /// <example>Arteriovenous fistula creation</example>
    public string? Display { get; set; }

    /// <summary>FHIR procedure status: preparation | in-progress | completed | not-done | stopped.</summary>
    /// <example>completed</example>
    public string Status { get; set; } = "completed";

    /// <summary>When it was done (UTC, ISO-8601 with Z). The from filter of the list compares with this value.</summary>
    /// <example>2023-11-02T14:00:00Z</example>
    public string? PerformedDateTime { get; set; }

    /// <summary>Clinician who did it.</summary>
    /// <example>prac-2</example>
    public string? PerformerPractitionerId { get; set; }

    /// <summary>Encounter in which it was done.</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }
}
