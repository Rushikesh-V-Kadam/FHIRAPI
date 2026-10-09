namespace FHIRAPI.Constants;

/// <summary>
/// FHIR and Implementation Guide versions this API follows.
/// </summary>
public static class FhirVersions
{
    /// <summary>FHIR R4: the main API (/fhir/r4), the version the Da Vinci guides and CMS-0057-F use.</summary>
    public const string Fhir = "4.0.1";

    /// <summary>FHIR R5: the converted view of the same data (/fhir/r5, see Versioning/R5Converter).</summary>
    public const string FhirR5 = "5.0.0";

    /// <summary>US Core version named by CMS-0057-F for CRD/DTR/PAS 2.0.1.</summary>
    public const string UsCoreGuide = "http://hl7.org/fhir/us/core/ImplementationGuide/hl7.fhir.us.core|3.1.1";
}
