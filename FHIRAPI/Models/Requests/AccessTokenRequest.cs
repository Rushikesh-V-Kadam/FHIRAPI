using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Body of POST /internal/access-tokens (sent by the Payer Gateway).
/// </summary>
public class AccessTokenRequest
{
    // [Required(AllowEmptyStrings = true)] only marks the property as required in Swagger. The controller itself
    // checks for an empty value and answers 400 with the usual { "error", "error_description" } body.

    /// <summary>Required. Who will use the token, normally the payer's id.</summary>
    /// <example>PAYER001</example>
    [Required(AllowEmptyStrings = true)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>Required. The only patient the token may read (EHR patient id).</summary>
    /// <example>pat-1</example>
    [Required(AllowEmptyStrings = true)]
    public string PatientId { get; set; } = string.Empty;

    /// <summary>SMART scopes, separated by a space. Empty = read access to the patient's data (SmartScopes.PayerDefault).</summary>
    /// <example>patient/Patient.rs patient/Coverage.rs patient/Observation.rs</example>
    public string? Scope { get; set; }

    /// <summary>Token lifetime in minutes, from 1 to 15 (a smaller or larger value is changed to 1 or 15).</summary>
    /// <example>5</example>
    [DefaultValue(5)]
    public int Minutes { get; set; } = 5;
}
