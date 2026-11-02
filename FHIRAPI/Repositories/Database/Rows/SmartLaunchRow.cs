namespace FHIRAPI.Repositories.Database.Rows;

/// <summary>
/// One SMART launch = one row of table PA_SMART_LAUNCH. GET / PUT {Ehr:DataPath}/smart-launches/{launchId}.
/// </summary>
public class SmartLaunchRow
{
    /// <summary>Key. The launch id sent to the app. Column LAUNCH_ID.</summary>
    public string LaunchId { get; set; } = string.Empty;

    /// <summary>Payer whose app may use the launch; null = any. Column INSURANCE_PLAN_ID.</summary>
    public int? InsurancePlanId { get; set; }

    /// <summary>Column PATIENT_ID.</summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Column ENCOUNTER_ID.</summary>
    public string? EncounterId { get; set; }

    /// <summary>Column USER_PRACTITIONER_ID.</summary>
    public string? UserPractitionerId { get; set; }

    /// <summary>JSON text: array of references, e.g. ["ServiceRequest/ord-1","Coverage/cov-1"]. Column FHIR_CONTEXT_JSON.</summary>
    public string? FhirContextJson { get; set; }

    /// <summary>appContext from the payer's card (free text). Column APP_CONTEXT.</summary>
    public string? AppContext { get; set; }

    /// <summary>UTC. Column EXPIRES_AT.</summary>
    public DateTime ExpiresAt { get; set; }
}
