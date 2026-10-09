namespace FHIRAPI.Models.Ehr;

/// <summary>
/// Appointment from GET api/appointments/{appointmentId}.
/// Plain EHR JSON (camelCase) - not FHIR.
/// </summary>
public class EhrAppointmentDto
{
    public string AppointmentId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string Status { get; set; } = "proposed";
    public string? ServiceCode { get; set; }
    public string? ServiceCodeSystem { get; set; }
    public string? ServiceDisplay { get; set; }
    public string? StartDateTime { get; set; }
    public string? EndDateTime { get; set; }
    public string? PractitionerId { get; set; }
    public string? LocationId { get; set; }
    public string? OrderId { get; set; }
}
