using FHIRAPI.Constants;
using FHIRAPI.Models.Ehr;
using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Mapping;

/// <summary>
/// EHR -> FHIR for orders and appointments (Da Vinci CRD profiles).
/// One EHR order becomes ServiceRequest, DeviceRequest or MedicationRequest depending on its orderType.
/// Registered as a singleton.
/// </summary>
public class OrderMapper
{
    private readonly CodeSystemMapper _codes;

    /// <summary>Created by dependency injection.</summary>
    public OrderMapper(CodeSystemMapper codes)
    {
        _codes = codes;
    }

    /// <summary>
    /// FHIR resource type of an EHR order:
    /// "device" -> DeviceRequest, "medication" -> MedicationRequest, anything else -> ServiceRequest.
    /// </summary>
    public static string GetResourceType(string? orderType) => orderType?.ToLowerInvariant() switch
    {
        "device" => "DeviceRequest",
        "medication" => "MedicationRequest",
        _ => "ServiceRequest"
    };

    /// <summary>Converts an order to the right FHIR request resource.</summary>
    public Resource ToFhir(EhrOrderDto order) => GetResourceType(order.OrderType) switch
    {
        "DeviceRequest" => ToDeviceRequest(order),
        "MedicationRequest" => ToMedicationRequest(order),
        _ => ToServiceRequest(order)
    };

    /// <summary>Service order (procedure, imaging, therapy) -> CRD ServiceRequest.</summary>
    public ServiceRequest ToServiceRequest(EhrOrderDto order)
    {
        var request = new ServiceRequest
        {
            Id = order.OrderId,
            Status = CodeSystemMapper.RequestStatus(order.Status),
            Priority = CodeSystemMapper.RequestPriority(order.Priority),
            Code = _codes.Concept(order.Code, order.CodeSystem, order.CodeDisplay),
            QuantityQuantity = order.Quantity == null ? null : new Quantity { Value = order.Quantity },
            Subject = new ResourceReference("Patient", order.PatientId),
            Encounter = Ref("Encounter", order.EncounterId),
            OccurrenceDateTime = GetOccurrenceDate(order),
            OccurrencePeriod = GetOccurrencePeriod(order),
            AuthoredOn = order.OrderedDateTime,
            Requester = Ref("Practitioner", order.OrderingPractitionerId)
        };
        request.AddProfile(FhirProfiles.CrdServiceRequest);

        AddIfPresent(request.Performer, Ref("Organization", order.PerformerOrganizationId));
        AddIfPresent(request.Performer, Ref("Practitioner", order.PerformerPractitionerId));
        AddIfPresent(request.LocationReference, Ref("Location", order.LocationId));
        if (!string.IsNullOrEmpty(order.PlaceOfServiceCode))
            request.LocationCode.Add(new CodeableConcept(CodeSystems.PlaceOfService, order.PlaceOfServiceCode));

        request.ReasonCode.AddRange(GetDiagnoses(order));
        AddIfPresent(request.Insurance, Ref("Coverage", order.CoverageId));
        if (!string.IsNullOrEmpty(order.Notes)) request.Note.Add(new Annotation { Text = order.Notes });
        return request;
    }

    /// <summary>Device order (DME) -> CRD DeviceRequest.</summary>
    public DeviceRequest ToDeviceRequest(EhrOrderDto order)
    {
        var request = new DeviceRequest
        {
            Id = order.OrderId,
            Status = CodeSystemMapper.RequestStatus(order.Status),
            Priority = CodeSystemMapper.RequestPriority(order.Priority),
            CodeCodeableConcept = _codes.Concept(order.Code, order.CodeSystem, order.CodeDisplay),
            Subject = new ResourceReference("Patient", order.PatientId),
            Encounter = Ref("Encounter", order.EncounterId),
            OccurrenceDateTime = GetOccurrenceDate(order),
            OccurrencePeriod = GetOccurrencePeriod(order),
            AuthoredOn = order.OrderedDateTime,
            Requester = Ref("Practitioner", order.OrderingPractitionerId),
            // DeviceRequest allows one performer: prefer the supplier organization
            Performer = Ref("Organization", order.PerformerOrganizationId) ?? Ref("Practitioner", order.PerformerPractitionerId)
        };
        request.AddProfile(FhirProfiles.CrdDeviceRequest);

        request.ReasonCode.AddRange(GetDiagnoses(order));
        AddIfPresent(request.Insurance, Ref("Coverage", order.CoverageId));
        if (!string.IsNullOrEmpty(order.Notes)) request.Note.Add(new Annotation { Text = order.Notes });
        return request;
    }

    /// <summary>Medication order -> CRD MedicationRequest (dose text, quantity, days supply, refills, pharmacy).</summary>
    public MedicationRequest ToMedicationRequest(EhrOrderDto order)
    {
        var request = new MedicationRequest
        {
            Id = order.OrderId,
            Status = CodeSystemMapper.RequestStatus(order.Status),
            Priority = CodeSystemMapper.RequestPriority(order.Priority),
            MedicationCodeableConcept = _codes.Concept(order.Code, order.CodeSystem, order.CodeDisplay),
            Subject = new ResourceReference("Patient", order.PatientId),
            Encounter = Ref("Encounter", order.EncounterId),
            AuthoredOn = order.OrderedDateTime,
            Requester = Ref("Practitioner", order.OrderingPractitionerId)
        };
        request.AddProfile(FhirProfiles.CrdMedicationRequest);

        var medication = order.Medication;
        if (!string.IsNullOrEmpty(medication?.DoseText))
            request.DosageInstruction.Add(new Dosage { Text = medication.DoseText });

        if (order.Quantity != null || medication != null)
        {
            request.DispenseRequest = new MedicationDispenseRequest
            {
                Quantity = order.Quantity == null ? null : new Quantity { Value = order.Quantity },
                NumberOfRepeatsAllowed = medication?.Refills,
                ExpectedSupplyDuration = medication?.DaysSupply == null
                    ? null
                    : new Quantity { Value = medication.DaysSupply, Unit = "days", System = CodeSystems.Ucum, Code = "d" },
                Performer = Ref("Organization", medication?.PharmacyOrganizationId)
            };
        }

        request.ReasonCode.AddRange(GetDiagnoses(order));
        AddIfPresent(request.Insurance, Ref("Coverage", order.CoverageId));
        if (!string.IsNullOrEmpty(order.Notes)) request.Note.Add(new Annotation { Text = order.Notes });
        return request;
    }

    /// <summary>EHR appointment -> CRD Appointment (patient, practitioner and location as participants).</summary>
    public Appointment ToAppointment(EhrAppointmentDto source)
    {
        var appointment = new Appointment
        {
            Id = source.AppointmentId,
            Status = source.Status,
            Start = source.StartDateTime,
            End = source.EndDateTime
        };
        appointment.AddProfile(FhirProfiles.CrdAppointment);

        if (!string.IsNullOrEmpty(source.ServiceCode))
            appointment.ServiceType.Add(_codes.Concept(source.ServiceCode, source.ServiceCodeSystem, source.ServiceDisplay));

        AddIfPresent(appointment.BasedOn, Ref("ServiceRequest", source.OrderId));

        appointment.Participant.Add(new AppointmentParticipant { Actor = new ResourceReference("Patient", source.PatientId) });
        if (!string.IsNullOrEmpty(source.PractitionerId))
            appointment.Participant.Add(new AppointmentParticipant { Actor = new ResourceReference("Practitioner", source.PractitionerId) });
        if (!string.IsNullOrEmpty(source.LocationId))
            appointment.Participant.Add(new AppointmentParticipant { Actor = new ResourceReference("Location", source.LocationId) });
        return appointment;
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Single requested date (used when there is no real date range).</summary>
    private static string? GetOccurrenceDate(EhrOrderDto order) =>
        GetOccurrencePeriod(order) == null ? order.RequestedStartDate : null;

    /// <summary>Date range, only when start and end are both given and different.</summary>
    private static Period? GetOccurrencePeriod(EhrOrderDto order)
    {
        var hasRange = order.RequestedStartDate != null && order.RequestedEndDate != null
                       && order.RequestedStartDate != order.RequestedEndDate;
        return hasRange ? new Period { Start = order.RequestedStartDate, End = order.RequestedEndDate } : null;
    }

    /// <summary>Order diagnoses as ICD-10-CM concepts.</summary>
    private List<CodeableConcept> GetDiagnoses(EhrOrderDto order) =>
        order.Diagnoses.Select(d => _codes.Concept(d.Code, d.CodeSystem ?? "ICD10CM", d.Display)).ToList();

    /// <summary>"Type/id" reference, or null when the id is empty.</summary>
    private static ResourceReference? Ref(string type, string? id) =>
        string.IsNullOrEmpty(id) ? null : new ResourceReference(type, id);

    /// <summary>Adds the reference to the list when it is not null.</summary>
    private static void AddIfPresent(List<ResourceReference> list, ResourceReference? reference)
    {
        if (reference != null) list.Add(reference);
    }
}
