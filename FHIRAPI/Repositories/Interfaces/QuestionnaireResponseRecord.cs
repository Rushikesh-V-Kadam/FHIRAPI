using System.Text.Json.Serialization;

namespace FHIRAPI.Repositories.Interfaces;

// CLIENT-EHR: the JSON names are the client's (GET Enterprise/getpasquestdtl, POST Enterprise/savepasquest), so this
// record is sent and read as it is - no separate DTO or mapping.
/// <summary>
/// One saved DTR form = one row of table PA_QUESTIONNAIRE_RESPONSE in the EHR database (saved through the client EHR API POST Enterprise/savepasquest;
/// every property is one column, e.g. LastUpdatedUtc = LAST_UPDATED_UTC).
/// The complete FHIR JSON is kept in ResourceJson; the other columns exist for searching and tracking.
/// </summary>
public class QuestionnaireResponseRecord
{
    /// <summary>FHIR id of the QuestionnaireResponse (primary key).</summary>
    [JsonPropertyName("ResponseID")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Patient the form is about (from QuestionnaireResponse.subject).</summary>
    [JsonPropertyName("PatientID")]
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Order the form was filled for (from the DTR qr-context extension).</summary>
    [JsonPropertyName("OrderID")]
    public string? OrderId { get; set; }

    /// <summary>Coverage the form was filled for (from the DTR qr-context extension).</summary>
    [JsonPropertyName("CoverageID")]
    public string? CoverageId { get; set; }

    /// <summary>Canonical URL of the Questionnaire.</summary>
    [JsonPropertyName("QuestionnaireURL")]
    public string? QuestionnaireUrl { get; set; }

    /// <summary>in-progress | completed | amended | entered-in-error | stopped</summary>
    [JsonPropertyName("Status")]
    public string Status { get; set; } = "in-progress";

    /// <summary>The complete FHIR QuestionnaireResponse JSON exactly as returned to readers.</summary>
    [JsonPropertyName("ResourceJSON")]
    public string ResourceJson { get; set; } = string.Empty;

    /// <summary>Id the EHR gave its copy of the form (needed to update the EHR copy later).</summary>
    [JsonPropertyName("EHRResponseID")]
    public string? EhrResponseId { get; set; }

    /// <summary>"SENT" when the EHR copy is up to date, "FAILED" when the EHR write-back failed (retry later).</summary>
    [JsonPropertyName("EHRSyncStatus")]
    public string EhrSyncStatus { get; set; } = "PENDING";

    /// <summary>When the row was last written (UTC).</summary>
    [JsonPropertyName("LastUpdatedUTC")]
    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}
