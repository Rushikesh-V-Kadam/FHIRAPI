using System.Text.Json;
using System.Text.Json.Serialization;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// Answer of GET api/prior-auth-data/payers/{insurancePlanId} in the client EHR's format:
/// { "PayerDetail": { ... }, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }. PayerDetail is null when there is no such payer.
/// </summary>
public class PayerDetailResponse
{
    /// <summary>The payer row with the client's property names (InsurancePlanID, PayerName, BaseURL ...), or null when there is no such payer.</summary>
    [JsonPropertyName("PayerDetail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]   // written as null, like the client EHR (the API ignores null properties otherwise)
    public Dictionary<string, JsonElement?>? PayerDetail { get; set; }

    /// <summary>Empty when the call worked. The client EHR fills it when its token expired.</summary>
    [JsonPropertyName("TOKEN_EXPIRED")]
    public string TokenExpired { get; set; } = string.Empty;

    /// <summary>Empty when the call worked; otherwise the reason it failed.</summary>
    [JsonPropertyName("ERROR_MESSAGE")]
    public string ErrorMessage { get; set; } = string.Empty;
}
