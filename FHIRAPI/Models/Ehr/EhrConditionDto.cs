namespace FHIRAPI.Models.Ehr;

// CLIENT-EHR: the row format of the client's diagnosis list API
//   GET Patients/getconlst?patientID={patientId} -> { "ConditionDetailList": [ ...rows... ], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
// Property names are matched without case.
/// <summary>
/// One diagnosis of a patient, as the client's EHR sends it. Plain EHR JSON - not FHIR.
/// </summary>
public class EhrConditionDto
{
    /// <summary>
    /// The EHR's id of the diagnosis. The client sends none for now; the FHIR Condition then has no id
    /// (Resource.id is 0..1 in FHIR R4).
    /// </summary>
    public string? ConditionId { get; set; }

    /// <summary>
    /// The patient of the diagnosis. The client is adding it to the rows; until then the patient id the list was read
    /// with is used.
    /// </summary>
    public string? PatientId { get; set; }

    /// <summary>
    /// Diagnosis code, e.g. M79.606 or 276.8. The client mixes ICD-10 and ICD-9 codes and sends no code system, so the
    /// code is sent without one.
    /// </summary>
    public string? DiagnosisCode { get; set; }

    /// <summary>Diagnosis text, e.g. PAIN IN LEG, UNSPECIFIED.</summary>
    public string? DiagnosisDescription { get; set; }

    /// <summary>problem-list-item or encounter-diagnosis.</summary>
    public string? Category { get; set; }

    /// <summary>Clinical status, e.g. active (FHIR condition-clinical).</summary>
    public string? DiagnosisStatus { get; set; }
}
