namespace FHIRAPI.Constants;

/// <summary>
/// Default SMART scopes.
/// </summary>
public static class SmartScopes
{
    /// <summary>
    /// Read access given to a payer during CRD (CDS Hooks fhirAuthorization).
    /// ".rs" = read + search (SMART v2).
    /// </summary>
    public const string PayerDefault =
        "patient/Patient.rs patient/Coverage.rs patient/Encounter.rs patient/Observation.rs patient/Condition.rs " +
        "patient/Procedure.rs patient/DocumentReference.rs patient/ServiceRequest.rs patient/DeviceRequest.rs " +
        "patient/MedicationRequest.rs patient/Appointment.rs user/Practitioner.rs user/PractitionerRole.rs " +
        "user/Organization.rs user/Location.rs";

    /// <summary>Used when the DTR app does not ask for a scope.</summary>
    public const string DtrDefault = "launch patient/*.rs patient/QuestionnaireResponse.cu";
}
