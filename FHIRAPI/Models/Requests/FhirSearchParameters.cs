using Microsoft.AspNetCore.Mvc;

namespace FHIRAPI.Models.Requests;

/// <summary>
/// Query parameters accepted by GET /fhir/r4/{type}.
/// Declared one by one so Swagger shows an input box for each of them.
/// Which parameters a resource type needs is listed in FhirSearchService.
/// </summary>
public class FhirSearchParameters
{
    /// <summary>One or more ids, comma separated: _id=ord-cpap,ord-visit</summary>
    [FromQuery(Name = "_id")]
    public string? Id { get; set; }

    /// <summary>Patient id or "Patient/id".</summary>
    [FromQuery(Name = "patient")]
    public string? Patient { get; set; }

    /// <summary>Same as patient (some clients send subject).</summary>
    [FromQuery(Name = "subject")]
    public string? Subject { get; set; }

    /// <summary>Same as patient (Coverage searches may send beneficiary).</summary>
    [FromQuery(Name = "beneficiary")]
    public string? Beneficiary { get; set; }

    /// <summary>Codes, comma separated, as system|code: http://loinc.org|39156-5</summary>
    [FromQuery(Name = "code")]
    public string? Code { get; set; }

    /// <summary>Observation category, e.g. laboratory or vital-signs.</summary>
    [FromQuery(Name = "category")]
    public string? Category { get; set; }

    /// <summary>Lower date bound: date=ge2026-01-01 (only ge/gt are used).</summary>
    [FromQuery(Name = "date")]
    public string? Date { get; set; }

    /// <summary>Coverage status, e.g. active.</summary>
    [FromQuery(Name = "status")]
    public string? Status { get; set; }

    /// <summary>Condition clinical status, e.g. active.</summary>
    [FromQuery(Name = "clinical-status")]
    public string? ClinicalStatus { get; set; }

    /// <summary>DocumentReference type as system|code: http://loinc.org|18748-4</summary>
    [FromQuery(Name = "type")]
    public string? Type { get; set; }

    /// <summary>
    /// Related resources to add to the Bundle, repeatable:
    /// _include=DeviceRequest:patient&amp;_include=DeviceRequest:requester
    /// </summary>
    [FromQuery(Name = "_include")]
    public List<string> Include { get; set; } = new();

    /// <summary>The patient id from patient, subject or beneficiary ("Patient/" prefix removed).</summary>
    public string? GetPatientId()
    {
        var value = Patient ?? Subject ?? Beneficiary;
        return value?.Split('/').Last();
    }

    /// <summary>The _id values as a list.</summary>
    public List<string> GetIds() =>
        (Id ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>"ge2026-01-01" -> "2026-01-01". Other prefixes are ignored.</summary>
    public string? GetDateFrom() =>
        Date != null && (Date.StartsWith("ge") || Date.StartsWith("gt")) ? Date[2..] : null;
}
