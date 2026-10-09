using FHIRAPI.Models.Fhir;

namespace FHIRAPI.Services;

/// <summary>
/// Answers two questions about a resource, without any JSON walking:
///  1. Which references does a search parameter point to? (used by _include)
///  2. Which patient does the resource belong to? (used to enforce patient-limited tokens)
/// </summary>
public static class ReferenceFinder
{
    /// <summary>A new empty list (a new one each time so callers can never change a shared list).</summary>
    private static List<ResourceReference> None => new();

    /// <summary>
    /// References behind an _include parameter, e.g. (DeviceRequest, "requester") -> [Practitioner/prac-1].
    /// Unknown combinations return an empty list (the include is ignored).
    /// </summary>
    public static List<ResourceReference> Find(Resource resource, string parameter)
    {
        switch (resource)
        {
            case ServiceRequest r:
                return parameter switch
                {
                    "patient" => One(r.Subject),
                    "requester" => One(r.Requester),
                    "performer" => r.Performer,
                    "encounter" => One(r.Encounter),
                    "location" => r.LocationReference,
                    "insurance" => r.Insurance,
                    _ => None
                };

            case DeviceRequest r:
                return parameter switch
                {
                    "patient" => One(r.Subject),
                    "requester" => One(r.Requester),
                    "performer" => One(r.Performer),
                    "encounter" => One(r.Encounter),
                    "insurance" => r.Insurance,
                    _ => None
                };

            case MedicationRequest r:
                return parameter switch
                {
                    "patient" => One(r.Subject),
                    "requester" => One(r.Requester),
                    "encounter" => One(r.Encounter),
                    "intended-dispenser" => One(r.DispenseRequest?.Performer),
                    "insurance" => r.Insurance,
                    _ => None
                };

            case Encounter r:
                return parameter switch
                {
                    "patient" => One(r.Subject),
                    "practitioner" => r.Participant.Select(p => p.Individual).OfType<ResourceReference>().ToList(),
                    "service-provider" => One(r.ServiceProvider),
                    "location" => r.Location.Select(l => l.Location).ToList(),
                    _ => None
                };

            case Appointment r:
                return parameter switch
                {
                    "patient" => ParticipantsOfType(r, "Patient"),
                    "practitioner" => ParticipantsOfType(r, "Practitioner"),
                    "location" => ParticipantsOfType(r, "Location"),
                    _ => None
                };

            case Coverage r:
                return parameter is "patient" or "beneficiary" ? One(r.Beneficiary) : None;

            case PractitionerRole r:
                return parameter switch
                {
                    "practitioner" => One(r.Practitioner),
                    "organization" => One(r.Organization),
                    _ => None
                };

            default:
                return None;
        }
    }

    /// <summary>
    /// The patient id a resource belongs to, or null for resources that are not patient data
    /// (Practitioner, Organization, Location...).
    /// </summary>
    public static string? GetPatientId(Resource resource) => resource switch
    {
        Patient r => r.Id,
        Coverage r => r.Beneficiary?.GetId(),
        ServiceRequest r => r.Subject?.GetId(),
        DeviceRequest r => r.Subject?.GetId(),
        MedicationRequest r => r.Subject?.GetId(),
        Encounter r => r.Subject?.GetId(),
        Observation r => r.Subject?.GetId(),
        Condition r => r.Subject?.GetId(),
        Procedure r => r.Subject?.GetId(),
        DocumentReference r => r.Subject?.GetId(),
        QuestionnaireResponse r => r.Subject?.GetId(),
        Appointment r => ParticipantsOfType(r, "Patient").FirstOrDefault()?.GetId(),
        _ => null
    };

    /// <summary>A list with the reference, or an empty list when it is null.</summary>
    private static List<ResourceReference> One(ResourceReference? reference) =>
        reference == null ? None : new List<ResourceReference> { reference };

    /// <summary>Appointment participants of one type, e.g. all "Practitioner/..." actors.</summary>
    private static List<ResourceReference> ParticipantsOfType(Appointment appointment, string type) =>
        appointment.Participant
            .Select(p => p.Actor)
            .OfType<ResourceReference>()
            .Where(a => a.GetResourceType() == type)
            .ToList();
}
