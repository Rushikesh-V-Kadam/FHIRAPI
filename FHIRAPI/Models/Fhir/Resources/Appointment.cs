using System.Text.Json.Serialization;

namespace FHIRAPI.Models.Fhir;

/// <summary>
/// FHIR Appointment (CRD profile). Used by the appointment-book hook.
/// </summary>
public class Appointment : DomainResource
{
    [JsonPropertyOrder(-100)] public override string ResourceType => "Appointment";

    public string Status { get; set; } = "proposed";
    public List<CodeableConcept> ServiceType { get; set; } = new();
    public string? Start { get; set; }
    public string? End { get; set; }
    /// <summary>The order the appointment is for.</summary>
    public List<ResourceReference> BasedOn { get; set; } = new();
    /// <summary>Patient, practitioner and location taking part.</summary>
    public List<AppointmentParticipant> Participant { get; set; } = new();
}
