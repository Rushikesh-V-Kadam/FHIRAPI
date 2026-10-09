namespace FHIRAPI.Constants;

/// <summary>
/// Profile URLs written into meta.profile of each resource we produce.
/// US Core 3.1.1 for clinical data, Da Vinci CRD 2.0.1 for orders and coverage.
/// </summary>
public static class FhirProfiles
{
    private const string UsCore = "http://hl7.org/fhir/us/core/StructureDefinition/";
    private const string Crd = "http://hl7.org/fhir/us/davinci-crd/StructureDefinition/";

    public const string Patient = UsCore + "us-core-patient";
    public const string Practitioner = UsCore + "us-core-practitioner";
    public const string PractitionerRole = UsCore + "us-core-practitionerrole";
    public const string Organization = UsCore + "us-core-organization";
    public const string Location = UsCore + "us-core-location";
    public const string Encounter = UsCore + "us-core-encounter";
    public const string Condition = UsCore + "us-core-condition";
    public const string Procedure = UsCore + "us-core-procedure";
    public const string ObservationLab = UsCore + "us-core-observation-lab";
    public const string VitalSigns = "http://hl7.org/fhir/StructureDefinition/vitalsigns";
    public const string DocumentReference = UsCore + "us-core-documentreference";
    public const string UsCoreRace = UsCore + "us-core-race";
    public const string UsCoreEthnicity = UsCore + "us-core-ethnicity";

    public const string CrdCoverage = Crd + "profile-coverage";
    public const string CrdServiceRequest = Crd + "profile-servicerequest";
    public const string CrdDeviceRequest = Crd + "profile-devicerequest";
    public const string CrdMedicationRequest = Crd + "profile-medicationrequest";
    public const string CrdAppointment = Crd + "profile-appointment";
}
