namespace FHIRAPI.Configuration;

/// <summary>
/// Identifier systems used when EHR data is converted to FHIR (appsettings section "FhirMapping").
/// Agree these values with each payer.
/// </summary>
public class FhirMappingSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "FhirMapping";

    /// <summary>System for the patient's medical record number (Patient.identifier).</summary>
    public string MrnSystem { get; set; } = "http://provider.example.org/fhir/sid/mrn";

    /// <summary>System for EHR document ids (DocumentReference.identifier).</summary>
    public string DocumentIdSystem { get; set; } = "http://provider.example.org/fhir/sid/document-id";

    /// <summary>System for payer ids (Coverage.payor.identifier).</summary>
    public string PayerIdSystem { get; set; } = "http://example.org/payer-ids";

    /// <summary>System for member ids (Coverage.identifier).</summary>
    public string MemberIdSystem { get; set; } = "http://example.org/member-ids";
}
