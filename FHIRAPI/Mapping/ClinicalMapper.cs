using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Models.Ehr;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Mapping;

/// <summary>
/// EHR -> FHIR for clinical data (US Core): Encounter, Observation, Condition, Procedure, DocumentReference, Binary.
/// Registered as a singleton.
/// </summary>
public class ClinicalMapper
{
    private readonly CodeSystemMapper _codes;
    private readonly FhirMappingSettings _settings;

    /// <summary>Created by dependency injection.</summary>
    public ClinicalMapper(CodeSystemMapper codes, FhirMappingSettings settings)
    {
        _codes = codes;
        _settings = settings;
    }

    /// <summary>EHR encounter -> US Core Encounter.</summary>
    public Encounter ToEncounter(EhrEncounterDto source)
    {
        var encounter = new Encounter
        {
            Id = source.EncounterId,
            Status = CodeSystemMapper.EncounterStatus(source.Status),
            Class = CodeSystemMapper.EncounterClass(source.EncounterClass),
            Subject = new ResourceReference("Patient", source.PatientId),
            Period = new Period { Start = source.StartDateTime, End = source.EndDateTime },
            ServiceProvider = string.IsNullOrEmpty(source.OrganizationId) ? null : new ResourceReference("Organization", source.OrganizationId)
        };
        encounter.AddProfile(FhirProfiles.Encounter);

        // US Core requires a type: use the code when there is one, otherwise text only
        encounter.Type.Add(string.IsNullOrEmpty(source.TypeCode)
            ? new CodeableConcept { Text = source.TypeDisplay ?? "Encounter" }
            : _codes.Concept(source.TypeCode, source.TypeCodeSystem, source.TypeDisplay));

        if (!string.IsNullOrEmpty(source.PractitionerId))
            encounter.Participant.Add(new EncounterParticipant { Individual = new ResourceReference("Practitioner", source.PractitionerId) });
        if (!string.IsNullOrEmpty(source.LocationId))
            encounter.Location.Add(new EncounterLocation { Location = new ResourceReference("Location", source.LocationId) });
        if (!string.IsNullOrEmpty(source.DischargeDisposition))
        {
            encounter.Hospitalization = new EncounterHospitalization
            {
                DischargeDisposition = new CodeableConcept(CodeSystems.DischargeDisposition, source.DischargeDisposition)
            };
        }

        encounter.ReasonCode.AddRange(source.ReasonDiagnoses.Select(d => _codes.Concept(d.Code, d.CodeSystem ?? "ICD10CM", d.Display)));
        return encounter;
    }

    /// <summary>
    /// EHR observation -> US Core lab Observation (or vital sign).
    /// Value: number -> valueQuantity, text -> valueString, code -> valueCodeableConcept, none -> dataAbsentReason.
    /// </summary>
    public Observation ToObservation(EhrObservationDto source)
    {
        var category = string.IsNullOrEmpty(source.Category) ? "laboratory" : source.Category.ToLowerInvariant();
        var observation = new Observation
        {
            Id = source.ObservationId,
            Status = source.Status,
            Code = _codes.Concept(source.Code, source.CodeSystem, source.Display),
            Subject = new ResourceReference("Patient", source.PatientId),
            Encounter = string.IsNullOrEmpty(source.EncounterId) ? null : new ResourceReference("Encounter", source.EncounterId),
            EffectiveDateTime = source.EffectiveDateTime,
            Issued = source.IssuedDateTime
        };
        observation.AddProfile(category == "vital-signs" ? FhirProfiles.VitalSigns : FhirProfiles.ObservationLab);
        observation.Category.Add(new CodeableConcept(CodeSystems.ObservationCategory, category));

        if (source.ValueNumber != null)
        {
            observation.ValueQuantity = new Quantity
            {
                Value = source.ValueNumber,
                Unit = source.Unit,
                System = source.Unit == null ? null : CodeSystems.Ucum,
                Code = source.Unit
            };
        }
        else if (source.ValueText != null)
        {
            observation.ValueString = source.ValueText;
        }
        else if (source.ValueCode != null)
        {
            observation.ValueCodeableConcept = _codes.Concept(source.ValueCode);
        }

        if (observation.ValueQuantity == null && observation.ValueString == null && observation.ValueCodeableConcept == null)
            observation.DataAbsentReason = new CodeableConcept(CodeSystems.DataAbsentReason, "unknown");

        return observation;
    }

    /// <summary>EHR condition -> US Core Condition.</summary>
    public Condition ToCondition(EhrConditionDto source)
    {
        var condition = new Condition
        {
            Id = source.ConditionId,
            Code = _codes.Concept(source.Code, source.CodeSystem, source.Display),
            Subject = new ResourceReference("Patient", source.PatientId),
            Encounter = string.IsNullOrEmpty(source.EncounterId) ? null : new ResourceReference("Encounter", source.EncounterId),
            OnsetDateTime = source.OnsetDate,
            RecordedDate = source.RecordedDate
        };
        condition.AddProfile(FhirProfiles.Condition);

        if (!string.IsNullOrEmpty(source.ClinicalStatus))
            condition.ClinicalStatus = new CodeableConcept(CodeSystems.ConditionClinical, source.ClinicalStatus.ToLowerInvariant());
        if (!string.IsNullOrEmpty(source.VerificationStatus))
            condition.VerificationStatus = new CodeableConcept(CodeSystems.ConditionVerification, source.VerificationStatus.ToLowerInvariant());

        var category = string.Equals(source.Category, "encounter-diagnosis", StringComparison.OrdinalIgnoreCase)
            ? "encounter-diagnosis"
            : "problem-list-item";
        condition.Category.Add(new CodeableConcept(CodeSystems.ConditionCategory, category));
        return condition;
    }

    /// <summary>EHR procedure -> US Core Procedure.</summary>
    public Procedure ToProcedure(EhrProcedureDto source)
    {
        var procedure = new Procedure
        {
            Id = source.ProcedureId,
            Status = source.Status,
            Code = _codes.Concept(source.Code, source.CodeSystem, source.Display),
            Subject = new ResourceReference("Patient", source.PatientId),
            Encounter = string.IsNullOrEmpty(source.EncounterId) ? null : new ResourceReference("Encounter", source.EncounterId),
            PerformedDateTime = source.PerformedDateTime
        };
        procedure.AddProfile(FhirProfiles.Procedure);

        if (!string.IsNullOrEmpty(source.PerformerPractitionerId))
            procedure.Performer.Add(new ProcedurePerformer { Actor = new ResourceReference("Practitioner", source.PerformerPractitionerId) });
        return procedure;
    }

    /// <summary>
    /// EHR document -> US Core DocumentReference.
    /// The file is not embedded; attachment.url points to {fhirBaseUrl}/Binary/{documentId} where it can be downloaded.
    /// </summary>
    public DocumentReference ToDocumentReference(EhrDocumentDto source, string fhirBaseUrl)
    {
        var document = new DocumentReference
        {
            Id = source.DocumentId,
            Status = source.Status,
            Type = _codes.Concept(source.TypeCode, source.TypeCodeSystem, source.TypeDisplay),
            Subject = new ResourceReference("Patient", source.PatientId),
            Date = source.CreatedDateTime,
            Description = source.Title
        };
        document.AddProfile(FhirProfiles.DocumentReference);

        document.Identifier.Add(new Identifier(_settings.DocumentIdSystem, source.DocumentId));
        var category = string.IsNullOrEmpty(source.Category) ? "clinical-note" : source.Category;
        document.Category.Add(new CodeableConcept(CodeSystems.DocumentCategory, category));

        if (!string.IsNullOrEmpty(source.AuthorPractitionerId))
            document.Author.Add(new ResourceReference("Practitioner", source.AuthorPractitionerId));

        document.Content.Add(new DocumentReferenceContent
        {
            Attachment = new Attachment
            {
                ContentType = source.ContentType,
                Title = source.Title,
                Creation = source.CreatedDateTime,
                Url = $"{fhirBaseUrl}/Binary/{source.DocumentId}"
            }
        });
        return document;
    }

    /// <summary>EHR document content -> FHIR Binary (base64 file).</summary>
    public static Binary ToBinary(EhrDocumentContentDto source) => new()
    {
        Id = source.DocumentId,
        ContentType = source.ContentType ?? "application/octet-stream",
        Data = source.ContentBase64
    };
}
