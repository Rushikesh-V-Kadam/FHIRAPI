namespace FHIRAPI.Models.Ehr;

// CLIENT-EHR: the row format of the client's procedure list API
//   GET Patients/getproclst?patientID={patientId} -> { "ProcedureDetailList": [ ...rows... ], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
// Property names are matched without case (ProcedureID, PerformerPractitionerNPI ...).
/// <summary>
/// One past procedure of a patient, as the client's EHR sends it. Plain EHR JSON - not FHIR.
/// </summary>
public class EhrProcedureDto
{
    /// <summary>
    /// The EHR's id of the procedure. The client sends it as null for now; the FHIR Procedure then has no id
    /// (Resource.id is 0..1 in FHIR R4).
    /// </summary>
    public string? ProcedureId { get; set; }

    /// <summary>
    /// The patient of the procedure. The client is adding it to the rows; until then the patient id the list was read
    /// with is used.
    /// </summary>
    public string? PatientId { get; set; }

    /// <summary>Procedure code, e.g. 90999. Sent without a code system (the client sends none).</summary>
    public string? ProcedureCode { get; set; }

    /// <summary>Procedure name, e.g. HEMODIALYSIS IN CENTER/COMPOSITE.</summary>
    public string? ProcedureName { get; set; }

    /// <summary>Procedure status, e.g. completed (FHIR event-status).</summary>
    public string? ProcedureStatus { get; set; }

    /// <summary>When it was done, e.g. 2026-04-20T00:00:00 (US Central time, no zone in the value).</summary>
    public string? PerformedDateTime { get; set; }

    /// <summary>NPI of the practitioner who did it, e.g. 1952887622.</summary>
    public string? PerformerPractitionerNpi { get; set; }
}
