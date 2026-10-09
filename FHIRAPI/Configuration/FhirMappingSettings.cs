namespace FHIRAPI.Configuration;

/// <summary>
/// Identifier systems used when EHR data is converted to FHIR (appsettings section "FhirMapping").
/// MrnSystem and DocumentIdSystem belong to the provider. PayerIdSystem and MemberIdSystem are optional: payers
/// normally do not give them, so they are empty and no "system" is sent with the payer id and the member id.
/// </summary>
public class FhirMappingSettings
{
    /// <summary>Name of the appsettings section.</summary>
    public const string SectionName = "FhirMapping";

    /// <summary>System for the patient's medical record number (Patient.identifier).</summary>
    public string MrnSystem { get; set; } = "http://provider.example.org/fhir/sid/mrn";

    /// <summary>System for EHR document ids (DocumentReference.identifier).</summary>
    public string DocumentIdSystem { get; set; } = "http://provider.example.org/fhir/sid/document-id";

    /// <summary>
    /// Optional. System for payer ids (Coverage.payor.identifier.system). Empty (the default) = the payer id is sent
    /// without a system. Set it only when a payer tells you which system its payer id must carry.
    /// </summary>
    public string PayerIdSystem { get; set; } = string.Empty;

    /// <summary>
    /// Optional. System for member ids (Coverage.identifier.system). Empty (the default) = the member id is sent
    /// without a system. Set it only when a payer tells you which system its member ids must carry.
    /// </summary>
    public string MemberIdSystem { get; set; } = string.Empty;
}
