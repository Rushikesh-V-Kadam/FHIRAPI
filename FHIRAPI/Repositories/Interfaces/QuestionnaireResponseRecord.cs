namespace FHIRAPI.Repositories.Interfaces;

/// <summary>
/// One saved DTR form = one row of table PA_QUESTIONNAIRE_RESPONSE in the EHR database (saved through the EHR data API;
/// every property is one column, e.g. LastUpdatedUtc = LAST_UPDATED_UTC).
/// The complete FHIR JSON is kept in ResourceJson; the other columns exist for searching and tracking.
/// </summary>
public class QuestionnaireResponseRecord
{
    /// <summary>FHIR id of the QuestionnaireResponse (primary key).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Patient the form is about (from QuestionnaireResponse.subject).</summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Order the form was filled for (from the DTR qr-context extension).</summary>
    public string? OrderId { get; set; }

    /// <summary>Coverage the form was filled for (from the DTR qr-context extension).</summary>
    public string? CoverageId { get; set; }

    /// <summary>Canonical URL of the Questionnaire.</summary>
    public string? QuestionnaireUrl { get; set; }

    /// <summary>in-progress | completed | amended | entered-in-error | stopped</summary>
    public string Status { get; set; } = "in-progress";

    /// <summary>The complete FHIR QuestionnaireResponse JSON exactly as returned to readers.</summary>
    public string ResourceJson { get; set; } = string.Empty;

    /// <summary>Id the EHR gave its copy of the form (needed to update the EHR copy later).</summary>
    public string? EhrResponseId { get; set; }

    /// <summary>"SENT" when the EHR copy is up to date, "FAILED" when the EHR write-back failed (retry later).</summary>
    public string EhrSyncStatus { get; set; } = "PENDING";

    /// <summary>When the row was last written (UTC).</summary>
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
