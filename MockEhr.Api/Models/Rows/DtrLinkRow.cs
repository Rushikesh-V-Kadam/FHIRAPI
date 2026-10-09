using System.ComponentModel.DataAnnotations;

namespace MockEhr.Api.Models.Rows;

/// <summary>
/// One payer SMART DTR app link given to the EHR = one row of table PA_DTR_LINK.
/// Body of PUT api/prior-auth-data/dtr-links/{linkId}; returned by GET of the same path.
/// Opening the link at the gateway uses these values to start the payer's DTR app.
/// </summary>
public class DtrLinkRow
{
    /// <summary>Key. Created by the gateway (the same value as in the URL). Column LINK_ID, VARCHAR2(64).</summary>
    /// <example>514ff06354c7485a907aec22535743f2</example>
    public string LinkId { get; set; } = string.Empty;

    /// <summary>Payer whose app is opened. Column PAYER_NAME, VARCHAR2(64).</summary>
    /// <example>MockPayer</example>
    public string PayerName { get; set; } = string.Empty;

    /// <summary>EHR patient id. Column PATIENT_ID, VARCHAR2(64).</summary>
    /// <example>pat-1</example>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>Encounter in context, if any. Column ENCOUNTER_ID, VARCHAR2(64).</summary>
    /// <example>enc-1</example>
    public string? EncounterId { get; set; }

    /// <summary>Clinician who signed the order. Column USER_PRACTITIONER_ID, VARCHAR2(64).</summary>
    /// <example>prac-1</example>
    public string? UserPractitionerId { get; set; }

    /// <summary>JSON text: array of the FHIR references the app works on. Column FHIR_CONTEXT_JSON, CLOB.</summary>
    /// <example>["ServiceRequest/ord-1-hd","Coverage/cov-1"]</example>
    public string? FhirContextJson { get; set; }

    /// <summary>Free text from the payer's CRD card (often JSON). Store unchanged and return unchanged. Column APP_CONTEXT, CLOB.</summary>
    /// <example>{"coverage-assertion-id":"702644806bf74c70aa65d2ca1a4a34b1","questionnaire":["http://example.org/fhir/Questionnaire/dialysis-incenter-hd"]}</example>
    public string? AppContext { get; set; }

    /// <summary>Launch URL of the DTR app. Column APP_URL, VARCHAR2(1000).</summary>
    /// <example>https://dtr.example.org/launch</example>
    public string AppUrl { get; set; } = string.Empty;

    /// <summary>The link cannot be opened after this time (UTC, ISO-8601 with Z). Column EXPIRES_AT, TIMESTAMP.</summary>
    /// <example>2026-10-10T18:35:40.1234567Z</example>
    [Required]
    public DateTime ExpiresAt { get; set; }
}
