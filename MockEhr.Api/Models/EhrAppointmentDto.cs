namespace MockEhr.Api.Models;

/// <summary>
/// An appointment, as the EHR returns it from GET api/appointments/{appointmentId}. Plain JSON (camelCase), not FHIR.
/// The FHIR API turns it into a FHIR Appointment.
/// </summary>
public class EhrAppointmentDto
{
    /// <summary>EHR appointment id.</summary>
    /// <example>appt-1</example>
    public string AppointmentId { get; set; } = string.Empty;

    /// <summary>Patient the appointment is for.</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>FHIR appointment status: proposed | pending | booked | arrived | fulfilled | cancelled | noshow.</summary>
    /// <example>booked</example>
    public string Status { get; set; } = "proposed";

    /// <summary>Service that is planned.</summary>
    /// <example>90935</example>
    public string? ServiceCode { get; set; }

    /// <summary>Code system of serviceCode: CPT or HCPCS.</summary>
    /// <example>CPT</example>
    public string? ServiceCodeSystem { get; set; }

    /// <summary>Text of the service.</summary>
    /// <example>In-center hemodialysis</example>
    public string? ServiceDisplay { get; set; }

    /// <summary>Start (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-10-05T13:00:00Z</example>
    public string? StartDateTime { get; set; }

    /// <summary>End (UTC, ISO-8601 with Z).</summary>
    /// <example>2026-10-05T17:00:00Z</example>
    public string? EndDateTime { get; set; }

    /// <summary>Clinician of the appointment.</summary>
    /// <example>prac-1</example>
    public string? PractitionerId { get; set; }

    /// <summary>Where the appointment takes place.</summary>
    /// <example>loc-main</example>
    public string? LocationId { get; set; }

    /// <summary>Order the appointment is based on.</summary>
    /// <example>ord-1-hd</example>
    public string? OrderId { get; set; }
}
