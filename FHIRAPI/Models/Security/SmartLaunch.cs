using FHIRAPI.Helpers;

namespace FHIRAPI.Models.Security;

/// <summary>
/// A pending SMART "EHR launch" for the DTR app, created by POST /internal/smart-launches.
/// Its data is returned to the app by the token endpoint (patient, encounter, fhirContext, appContext).
/// </summary>
public class SmartLaunch
{
    /// <summary>Random id the DTR app receives as the "launch" parameter.</summary>
    public string LaunchId { get; set; } = RandomIds.New(18);

    public string PatientId { get; set; } = string.Empty;
    public string? UserPractitionerId { get; set; }
    public string? EncounterId { get; set; }

    /// <summary>e.g. "DeviceRequest/ord-cpap", "Coverage/cov-1".</summary>
    public List<string> FhirContext { get; set; } = new();

    /// <summary>appContext from the payer's CRD card.</summary>
    public string? AppContext { get; set; }


    /// <summary>"r4" or "r5": the release the launch was created for (the "iss" sent to the app).</summary>
    public string FhirVersion { get; set; } = "r4";

    /// <summary>A launch must be used within 15 minutes.</summary>
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(15);
}
