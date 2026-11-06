using System.Text.Json.Serialization;

namespace MockEhr.Api.Models.Client;

/// <summary>
/// The client EHR's answer for a list: { "&lt;EntityName&gt;": [ ...rows... ], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.
/// The entity name differs per API (ProcedureDetailList, ConditionDetailList, ObservationDetailList), so each list API
/// has its own small class below.
/// </summary>
public abstract class ClientStatus
{
    /// <summary>Empty when the call worked. The client EHR fills it when its token expired.</summary>
    [JsonPropertyName("TOKEN_EXPIRED")]
    [JsonPropertyOrder(100)]
    public string TokenExpired { get; set; } = string.Empty;

    /// <summary>Empty when the call worked; otherwise the reason it failed.</summary>
    [JsonPropertyName("ERROR_MESSAGE")]
    [JsonPropertyOrder(101)]
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>Answer of GET Patients/getproclst: { "ProcedureDetailList": [...], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</summary>
public class ProcedureListResponse : ClientStatus
{
    /// <summary>The procedures of the patient.</summary>
    [JsonPropertyName("ProcedureDetailList")]
    public List<ClientProcedureRow> ProcedureDetailList { get; set; } = new List<ClientProcedureRow>();
}

/// <summary>Answer of GET Patients/getconlst: { "ConditionDetailList": [...], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</summary>
public class ConditionListResponse : ClientStatus
{
    /// <summary>The diagnoses of the patient.</summary>
    [JsonPropertyName("ConditionDetailList")]
    public List<ClientConditionRow> ConditionDetailList { get; set; } = new List<ClientConditionRow>();
}

/// <summary>Answer of GET Patients/getobslst: { "ObservationDetailList": [...], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.</summary>
public class ObservationListResponse : ClientStatus
{
    /// <summary>The lab results of the patient.</summary>
    [JsonPropertyName("ObservationDetailList")]
    public List<ClientObservationRow> ObservationDetailList { get; set; } = new List<ClientObservationRow>();
}
