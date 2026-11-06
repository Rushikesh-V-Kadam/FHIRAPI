namespace FHIRAPI.Models.Ehr;

// CLIENT-EHR: the row format of the client's lab result list API
//   GET Patients/getobslst?patientID={patientId} -> { "ObservationDetailList": [ ...rows... ], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
// Property names are matched without case.
/// <summary>
/// One lab result of a patient, as the client's EHR sends it. Plain EHR JSON - not FHIR.
/// </summary>
public class EhrObservationDto
{
    /// <summary>
    /// The EHR's id of the result. The client sends none for now; the FHIR Observation then has no id
    /// (Resource.id is 0..1 in FHIR R4).
    /// </summary>
    public string? ObservationId { get; set; }

    /// <summary>
    /// The patient of the result. The client is adding it to the rows; until then the patient id the list was read
    /// with is used.
    /// </summary>
    public string? PatientId { get; set; }

    /// <summary>Result status, e.g. final (FHIR observation-status).</summary>
    public string? LabStatus { get; set; }

    /// <summary>Test code, e.g. 2502-3. Sent without a code system (the client sends none).</summary>
    public string? LabTestCode { get; set; }

    /// <summary>Test name, e.g. IRON SATURATION (% IRON).</summary>
    public string? LabTestName { get; set; }

    /// <summary>When it was measured, e.g. 2026-04-06T00:01:00 (US Central time, no zone in the value).</summary>
    public string? EffectiveDate { get; set; }

    /// <summary>The result as text, e.g. "64.2". A number becomes valueQuantity, any other text valueString.</summary>
    public string? LabResultValue { get; set; }

    /// <summary>Result description. The client sends the same text as LabTestName, so it is not used.</summary>
    public string? LabResultDescription { get; set; }

    /// <summary>Unit as the EHR writes it, e.g. ng/mL. Not checked against UCUM.</summary>
    public string? LabUnit { get; set; }
}
