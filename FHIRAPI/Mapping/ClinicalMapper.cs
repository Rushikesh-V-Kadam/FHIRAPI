using System.Globalization;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;
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

    /// <summary>Created by dependency injection.</summary>
    public ClinicalMapper(CodeSystemMapper codes)
    {
        _codes = codes;
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

    // CLIENT-EHR: the client's rows have no code system for lab, diagnosis and procedure codes, so the codes are sent
    // without a system (Coding.system is 0..1 in FHIR R4).
    // TODO: when the client confirms the code systems (e.g. LOINC for labs, CPT for procedures, ICD-10 for diagnoses),
    // send them: payer DTR forms usually look data up by system|code.

    /// <summary>FHIR observation-status codes (Observation.status, 1..1).</summary>
    private static readonly HashSet<string> ObservationStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "registered", "preliminary", "final", "amended", "corrected", "cancelled", "entered-in-error", "unknown"
    };

    /// <summary>FHIR event-status codes (Procedure.status, 1..1).</summary>
    private static readonly HashSet<string> ProcedureStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "preparation", "in-progress", "not-done", "on-hold", "stopped", "completed", "entered-in-error", "unknown"
    };

    /// <summary>FHIR condition-clinical codes (Condition.clinicalStatus, 0..1).</summary>
    private static readonly HashSet<string> ConditionClinicalStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        "active", "recurrence", "relapse", "inactive", "remission", "resolved"
    };

    /// <summary>
    /// EHR lab result -> US Core lab Observation.
    /// Value: a number -> valueQuantity (with the EHR unit as text), other text -> valueString, none -> dataAbsentReason.
    /// patientId is the patient the list was read with (used when the row has no PatientId).
    /// </summary>
    public Observation ToObservation(EhrObservationDto source, string patientId)
    {
        Observation observation = new Observation();
        // CLIENT-EHR: the client's rows have no id yet; the Observation is then sent without an id.
        observation.Id = EmptyToNull(source.ObservationId);
        observation.Status = KnownCodeOr(source.LabStatus, ObservationStatuses, "unknown");
        observation.Code = CodeWithoutSystem(source.LabTestCode, source.LabTestName);
        observation.Subject = new ResourceReference("Patient", RowPatient(source.PatientId, patientId));
        // CLIENT-EHR: EffectiveDate is US Central time without a zone; the zone is added.
        observation.EffectiveDateTime = EhrDateTime.ToFhir(source.EffectiveDate);
        observation.AddProfile(FhirProfiles.ObservationLab);
        // CLIENT-EHR: the client's list API returns lab results only (it has no category field).
        observation.Category.Add(new CodeableConcept(CodeSystems.ObservationCategory, "laboratory"));

        string? value = EmptyToNull(source.LabResultValue);
        decimal number;
        if (value != null && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number))
        {
            Quantity quantity = new Quantity();
            quantity.Value = number;
            // CLIENT-EHR: the unit is the EHR's text (e.g. ng/mL); it is not checked against UCUM, so no system/code is sent.
            quantity.Unit = EmptyToNull(source.LabUnit);
            observation.ValueQuantity = quantity;
        }
        else if (value != null)
        {
            observation.ValueString = value;
        }
        else
        {
            observation.DataAbsentReason = new CodeableConcept(CodeSystems.DataAbsentReason, "unknown");
        }
        return observation;
    }

    /// <summary>
    /// EHR diagnosis -> US Core Condition.
    /// patientId is the patient the list was read with (used when the row has no PatientId).
    /// </summary>
    public Condition ToCondition(EhrConditionDto source, string patientId)
    {
        Condition condition = new Condition();
        // CLIENT-EHR: the client's rows have no id yet; the Condition is then sent without an id.
        condition.Id = EmptyToNull(source.ConditionId);
        // CLIENT-EHR: the client mixes ICD-10 and ICD-9 codes and sends no code system.
        condition.Code = CodeWithoutSystem(source.DiagnosisCode, source.DiagnosisDescription);
        condition.Subject = new ResourceReference("Patient", RowPatient(source.PatientId, patientId));
        condition.AddProfile(FhirProfiles.Condition);

        string? status = EmptyToNull(source.DiagnosisStatus);
        if (status != null && ConditionClinicalStatuses.Contains(status.ToLowerInvariant()))
        {
            condition.ClinicalStatus = new CodeableConcept(CodeSystems.ConditionClinical, status.ToLowerInvariant());
        }

        string category = "problem-list-item";
        if (string.Equals(source.Category, "encounter-diagnosis", StringComparison.OrdinalIgnoreCase))
        {
            category = "encounter-diagnosis";
        }
        condition.Category.Add(new CodeableConcept(CodeSystems.ConditionCategory, category));
        return condition;
    }

    /// <summary>
    /// EHR procedure -> US Core Procedure.
    /// patientId is the patient the list was read with (used when the row has no PatientId).
    /// </summary>
    public Procedure ToProcedure(EhrProcedureDto source, string patientId)
    {
        Procedure procedure = new Procedure();
        // CLIENT-EHR: the client sends ProcedureID as null for now; the Procedure is then sent without an id.
        procedure.Id = EmptyToNull(source.ProcedureId);
        procedure.Status = KnownCodeOr(source.ProcedureStatus, ProcedureStatuses, "unknown");
        procedure.Code = CodeWithoutSystem(source.ProcedureCode, source.ProcedureName);
        procedure.Subject = new ResourceReference("Patient", RowPatient(source.PatientId, patientId));
        // CLIENT-EHR: PerformedDateTime is US Central time without a zone; the zone is added.
        procedure.PerformedDateTime = EhrDateTime.ToFhir(source.PerformedDateTime);
        procedure.AddProfile(FhirProfiles.Procedure);

        // CLIENT-EHR: the performer comes as an NPI, not as an EHR practitioner id, so it is a reference by identifier.
        string? npi = EmptyToNull(source.PerformerPractitionerNpi);
        if (npi != null)
        {
            ResourceReference actor = new ResourceReference();
            actor.Identifier = new Identifier(CodeSystems.Npi, npi);
            ProcedurePerformer performer = new ProcedurePerformer();
            performer.Actor = actor;
            procedure.Performer.Add(performer);
        }
        return procedure;
    }

    /// <summary>A code without a code system, with the EHR text as display and text. Empty when there is no code.</summary>
    private static CodeableConcept CodeWithoutSystem(string? code, string? display)
    {
        string? text = EmptyToNull(display);
        if (string.IsNullOrWhiteSpace(code))
        {
            CodeableConcept textOnly = new CodeableConcept();
            textOnly.Text = text;
            return textOnly;
        }
        CodeableConcept concept = new CodeableConcept(null, code.Trim(), text);
        concept.Text = text;
        return concept;
    }

    /// <summary>The row's patient id when it has one, otherwise the patient the list was read with.</summary>
    private static string RowPatient(string? rowPatientId, string listPatientId)
    {
        if (string.IsNullOrWhiteSpace(rowPatientId))
        {
            return listPatientId;
        }
        return rowPatientId.Trim();
    }

    /// <summary>The lower-case value when it is one of the allowed codes, otherwise the fallback code.</summary>
    private static string KnownCodeOr(string? value, HashSet<string> allowed, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }
        string code = value.Trim().ToLowerInvariant();
        if (allowed.Contains(code))
        {
            return code;
        }
        return fallback;
    }

    /// <summary>Null for an empty or blank text, otherwise the trimmed text.</summary>
    private static string? EmptyToNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return value.Trim();
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

        // The EHR document id, sent without a "system" (Identifier.system is 0..1 in FHIR R4)
        document.Identifier.Add(new Identifier { Value = source.DocumentId });
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
