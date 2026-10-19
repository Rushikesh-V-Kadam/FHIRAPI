using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Constants;
using FHIRAPI.Models.Requests;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// FHIR R4 read and search of Clinical data and documents.
/// One action per resource type (GET /fhir/r4/{Type}/{id} and GET /fhir/r4/{Type}?...), so each resource has
/// its own URL and its own search parameters in Swagger. Everything is fetched live from the EHR and converted;
/// nothing is stored. Callers: payers during CRD, payer DTR apps and the Payer Gateway. No authentication is built in.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Produces(FhirMediaTypes.FhirJson)]
public class FhirClinicalController : FhirResourceControllerBase
{
    /// <summary>Created by dependency injection.</summary>
    public FhirClinicalController(FhirReadService reader, FhirSearchService searcher)
        : base(reader, searcher)
    {
    }

    /// <summary>Searches Observation and returns a "searchset" Bundle. Required: patient (or subject / beneficiary). GET /fhir/r4/Observation?...</summary>
    /// <param name="patient" example="pat-1">Required, or send subject. Patient id, or "Patient/{id}".</param>
    /// <param name="subject">Optional. Same as patient (some clients send subject).</param>
    /// <param name="code">Optional. Codes, comma separated, as system|code, e.g. http://loinc.org|39156-5.</param>
    /// <param name="category">Optional. Observation category, e.g. laboratory or vital-signs.</param>
    /// <param name="date">Optional. Lower date bound: ge2026-01-01 (only ge and gt are used).</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Observation:parameter[:TargetType], e.g. Observation:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Observation")]
    [Tags("1. FHIR APIs - Observation")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchObservation([FromQuery(Name = "patient")] string? patient, [FromQuery(Name = "subject")] string? subject, [FromQuery(Name = "code")] string? code, [FromQuery(Name = "category")] string? category, [FromQuery(Name = "date")] string? date, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Patient = patient;
        search.Subject = subject;
        search.Code = code;
        search.Category = category;
        search.Date = date;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Observation", search, ct);
    }

    /// <summary>Searches Condition and returns a "searchset" Bundle. Required: patient (or subject / beneficiary). GET /fhir/r4/Condition?...</summary>
    /// <param name="patient" example="pat-1">Required, or send subject. Patient id, or "Patient/{id}".</param>
    /// <param name="subject">Optional. Same as patient (some clients send subject).</param>
    /// <param name="code">Optional. Codes, comma separated, as system|code, e.g. http://loinc.org|39156-5.</param>
    /// <param name="clinicalStatus">Optional. Condition clinical status, e.g. active.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Condition:parameter[:TargetType], e.g. Condition:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Condition")]
    [Tags("1. FHIR APIs - Condition")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchCondition([FromQuery(Name = "patient")] string? patient, [FromQuery(Name = "subject")] string? subject, [FromQuery(Name = "code")] string? code, [FromQuery(Name = "clinical-status")] string? clinicalStatus, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Patient = patient;
        search.Subject = subject;
        search.Code = code;
        search.ClinicalStatus = clinicalStatus;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Condition", search, ct);
    }

    /// <summary>Searches Procedure and returns a "searchset" Bundle. Required: patient (or subject / beneficiary). GET /fhir/r4/Procedure?...</summary>
    /// <param name="patient" example="pat-1">Required, or send subject. Patient id, or "Patient/{id}".</param>
    /// <param name="subject">Optional. Same as patient (some clients send subject).</param>
    /// <param name="code">Optional. Codes, comma separated, as system|code, e.g. http://loinc.org|39156-5.</param>
    /// <param name="date">Optional. Lower date bound: ge2026-01-01 (only ge and gt are used).</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Procedure:parameter[:TargetType], e.g. Procedure:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Procedure")]
    [Tags("1. FHIR APIs - Procedure")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchProcedure([FromQuery(Name = "patient")] string? patient, [FromQuery(Name = "subject")] string? subject, [FromQuery(Name = "code")] string? code, [FromQuery(Name = "date")] string? date, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Patient = patient;
        search.Subject = subject;
        search.Code = code;
        search.Date = date;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Procedure", search, ct);
    }

    /// <summary>Reads one DocumentReference by id. GET /fhir/r4/DocumentReference/{id}</summary>
    /// <param name="id" example="doc-1-labs">Required. EHR document id.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The DocumentReference as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("DocumentReference/{id}")]
    [Tags("1. FHIR APIs - DocumentReference")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadDocumentReference(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("DocumentReference", id, ct);
    }

    /// <summary>Searches DocumentReference and returns a "searchset" Bundle. Required: patient (or subject / beneficiary). GET /fhir/r4/DocumentReference?...</summary>
    /// <param name="patient" example="pat-1">Required, or send subject. Patient id, or "Patient/{id}".</param>
    /// <param name="subject">Optional. Same as patient (some clients send subject).</param>
    /// <param name="documentType">Optional. Document type as system|code, e.g. http://loinc.org|18748-4.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as DocumentReference:parameter[:TargetType], e.g. DocumentReference:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("DocumentReference")]
    [Tags("1. FHIR APIs - DocumentReference")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchDocumentReference([FromQuery(Name = "patient")] string? patient, [FromQuery(Name = "subject")] string? subject, [FromQuery(Name = "type")] string? documentType, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Patient = patient;
        search.Subject = subject;
        search.Type = documentType;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("DocumentReference", search, ct);
    }

    /// <summary>Reads one Binary by id. GET /fhir/r4/Binary/{id}</summary>
    /// <param name="id" example="doc-1-labs">Required. EHR document id: the file of DocumentReference/{id}.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Binary as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Binary/{id}")]
    [Tags("1. FHIR APIs - Binary")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadBinary(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Binary", id, ct);
    }
}
