namespace FHIRAPI.Models.Fhir;

/// <summary>
/// Someone or something taking part in an Appointment.
/// </summary>
public class AppointmentParticipant
{
    public ResourceReference? Actor { get; set; }
    public string Status { get; set; } = "accepted";
}
