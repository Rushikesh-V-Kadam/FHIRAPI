namespace FHIRAPI.Constants;

/// <summary>
/// DTR 2.0.1 extension URLs found on QuestionnaireResponses saved by the DTR app.
/// </summary>
public static class DtrExtensions
{
    /// <summary>
    /// qr-context: which order and coverage the form was filled for,
    /// e.g. valueReference "DeviceRequest/ord-cpap" and "Coverage/cov-1".
    /// </summary>
    public const string QrContext = "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context";

    /// <summary>information-origin: whether an answer was auto-filled, overridden or entered manually.</summary>
    public const string InformationOrigin = "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/information-origin";
}
