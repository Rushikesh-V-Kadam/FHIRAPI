namespace FHIRAPI.Constants;

/// <summary>
/// FHIR code system and identifier system URIs used by the mappers.
/// </summary>
public static class CodeSystems
{
    // ---- clinical and billing codes
    public const string Cpt = "http://www.ama-assn.org/go/cpt";
    public const string Hcpcs = "https://bluebutton.cms.gov/resources/codesystem/hcpcs";
    public const string Loinc = "http://loinc.org";
    public const string Icd10Cm = "http://hl7.org/fhir/sid/icd-10-cm";
    public const string Snomed = "http://snomed.info/sct";
    public const string RxNorm = "http://www.nlm.nih.gov/research/umls/rxnorm";
    public const string Nucc = "http://nucc.org/provider-taxonomy";
    public const string PlaceOfService = "https://www.cms.gov/Medicare/Coding/place-of-service-codes/Place_of_Service_Code_Set";
    public const string Ucum = "http://unitsofmeasure.org";
    public const string OmbRaceEthnicity = "urn:oid:2.16.840.1.113883.6.238";

    // ---- identifier systems
    public const string Npi = "http://hl7.org/fhir/sid/us-npi";
    public const string TaxId = "urn:oid:2.16.840.1.113883.4.4";

    // ---- HL7 terminology
    public const string V2IdentifierType = "http://terminology.hl7.org/CodeSystem/v2-0203";
    public const string ActCode = "http://terminology.hl7.org/CodeSystem/v3-ActCode";
    public const string SubscriberRelationship = "http://terminology.hl7.org/CodeSystem/subscriber-relationship";
    public const string CoverageClass = "http://terminology.hl7.org/CodeSystem/coverage-class";
    public const string ObservationCategory = "http://terminology.hl7.org/CodeSystem/observation-category";
    public const string ConditionClinical = "http://terminology.hl7.org/CodeSystem/condition-clinical";
    public const string ConditionVerification = "http://terminology.hl7.org/CodeSystem/condition-ver-status";
    public const string ConditionCategory = "http://terminology.hl7.org/CodeSystem/condition-category";
    public const string DocumentCategory = "http://hl7.org/fhir/us/core/CodeSystem/us-core-documentreference-category";
    public const string DischargeDisposition = "http://terminology.hl7.org/CodeSystem/discharge-disposition";
    public const string DataAbsentReason = "http://terminology.hl7.org/CodeSystem/data-absent-reason";
}
