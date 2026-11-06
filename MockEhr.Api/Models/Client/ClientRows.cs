using System.Text.Json.Serialization;

namespace MockEhr.Api.Models.Client;

/// <summary>A procedure row with the client EHR's property names. Dates are US Central time without a zone.</summary>
public class ClientProcedureRow
{
    /// <summary>The client sends null for now.</summary>
    [JsonPropertyName("ProcedureID")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]   // written as null, like the client EHR
    public string? ProcedureId { get; set; }

    /// <summary>The patient (the client is adding this field).</summary>
    [JsonPropertyName("PatientID")]
    public string? PatientId { get; set; }

    /// <example>90999</example>
    [JsonPropertyName("ProcedureCode")]
    public string? ProcedureCode { get; set; }

    /// <example>HEMODIALYSIS IN CENTER/COMPOSITE</example>
    [JsonPropertyName("ProcedureName")]
    public string? ProcedureName { get; set; }

    /// <example>completed</example>
    [JsonPropertyName("ProcedureStatus")]
    public string? ProcedureStatus { get; set; }

    /// <example>2026-04-20T00:00:00</example>
    [JsonPropertyName("PerformedDateTime")]
    public string? PerformedDateTime { get; set; }

    /// <example>1952887622</example>
    [JsonPropertyName("PerformerPractitionerNPI")]
    public string? PerformerPractitionerNpi { get; set; }
}

/// <summary>A diagnosis row with the client EHR's property names (no id, no code system).</summary>
public class ClientConditionRow
{
    /// <summary>The patient (the client is adding this field).</summary>
    [JsonPropertyName("PatientID")]
    public string? PatientId { get; set; }

    /// <example>M79.606</example>
    [JsonPropertyName("DiagnosisCode")]
    public string? DiagnosisCode { get; set; }

    /// <example>PAIN IN LEG, UNSPECIFIED</example>
    [JsonPropertyName("DiagnosisDescription")]
    public string? DiagnosisDescription { get; set; }

    /// <example>problem-list-item</example>
    [JsonPropertyName("Category")]
    public string? Category { get; set; }

    /// <example>active</example>
    [JsonPropertyName("DiagnosisStatus")]
    public string? DiagnosisStatus { get; set; }
}

/// <summary>A lab result row with the client EHR's property names (no id, no code system, value as text).</summary>
public class ClientObservationRow
{
    /// <summary>The patient (the client is adding this field).</summary>
    [JsonPropertyName("PatientID")]
    public string? PatientId { get; set; }

    /// <example>final</example>
    [JsonPropertyName("LabStatus")]
    public string? LabStatus { get; set; }

    /// <example>2502-3</example>
    [JsonPropertyName("LabTestCode")]
    public string? LabTestCode { get; set; }

    /// <example>2026-04-06T00:01:00</example>
    [JsonPropertyName("EffectiveDate")]
    public string? EffectiveDate { get; set; }

    /// <example>IRON SATURATION (% IRON)</example>
    [JsonPropertyName("LabTestName")]
    public string? LabTestName { get; set; }

    /// <example>21</example>
    [JsonPropertyName("LabResultValue")]
    public string? LabResultValue { get; set; }

    /// <example>IRON SATURATION (% IRON)</example>
    [JsonPropertyName("LabResultDescription")]
    public string? LabResultDescription { get; set; }

    /// <example>%</example>
    [JsonPropertyName("LabUnit")]
    public string? LabUnit { get; set; }
}
