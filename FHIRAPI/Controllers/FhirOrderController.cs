using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Constants;
using FHIRAPI.Models.Requests;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// FHIR R4 read and search of Orders and encounters.
/// One action per resource type (GET /fhir/r4/{Type}/{id} and GET /fhir/r4/{Type}?...), so each resource has
/// its own URL and its own search parameters in Swagger. Everything is fetched live from the EHR and converted;
/// nothing is stored. Callers: payers during CRD, payer DTR apps and the Payer Gateway. No authentication is built in.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Produces(FhirMediaTypes.FhirJson)]
public class FhirOrderController : FhirResourceControllerBase
{
    /// <summary>Created by dependency injection.</summary>
    public FhirOrderController(FhirReadService reader, FhirSearchService searcher)
        : base(reader, searcher)
    {
    }

    /// <summary>Reads one ServiceRequest by id. GET /fhir/r4/ServiceRequest/{id}</summary>
    /// <param name="id" example="ord-1-hd">Required. EHR order id of a service order.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The ServiceRequest as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("ServiceRequest/{id}")]
    [Tags("FHIR APIs - ServiceRequest")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadServiceRequest(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("ServiceRequest", id, ct);
    }

    /// <summary>Searches ServiceRequest and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/ServiceRequest?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as ServiceRequest:parameter[:TargetType], e.g. ServiceRequest:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("ServiceRequest")]
    [Tags("FHIR APIs - ServiceRequest")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchServiceRequest([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("ServiceRequest", search, ct);
    }

    /// <summary>Reads one MedicationRequest by id. GET /fhir/r4/MedicationRequest/{id}</summary>
    /// <param name="id" example="ord-1-retacrit">Required. EHR order id of a medication order.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The MedicationRequest as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("MedicationRequest/{id}")]
    [Tags("FHIR APIs - MedicationRequest")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadMedicationRequest(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("MedicationRequest", id, ct);
    }

    /// <summary>Searches MedicationRequest and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/MedicationRequest?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as MedicationRequest:parameter[:TargetType], e.g. MedicationRequest:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("MedicationRequest")]
    [Tags("FHIR APIs - MedicationRequest")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchMedicationRequest([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("MedicationRequest", search, ct);
    }

    /// <summary>Reads one Encounter by id. GET /fhir/r4/Encounter/{id}</summary>
    /// <param name="id" example="enc-1">Required. EHR encounter id.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Encounter as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Encounter/{id}")]
    [Tags("FHIR APIs - Encounter")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadEncounter(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Encounter", id, ct);
    }

    /// <summary>Searches Encounter and returns a "searchset" Bundle. Required: _id, or patient (or subject). GET /fhir/r4/Encounter?...</summary>
    /// <param name="id">Required: _id or patient (or subject). One or more ids, comma separated.</param>
    /// <param name="patient" example="pat-1">Required: _id or patient (or subject). Patient id, or "Patient/{id}".</param>
    /// <param name="subject">Optional. Same as patient (some clients send subject).</param>
    /// <param name="date">Optional. Lower date bound: ge2026-01-01 (only ge and gt are used).</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Encounter:parameter[:TargetType], e.g. Encounter:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Encounter")]
    [Tags("FHIR APIs - Encounter")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchEncounter([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "patient")] string? patient, [FromQuery(Name = "subject")] string? subject, [FromQuery(Name = "date")] string? date, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Patient = patient;
        search.Subject = subject;
        search.Date = date;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Encounter", search, ct);
    }
}
