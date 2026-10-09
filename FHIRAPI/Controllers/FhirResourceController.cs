using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Fhir;
using FHIRAPI.Models.Requests;
using FHIRAPI.Services;
using FHIRAPI.Versioning;

namespace FHIRAPI.Controllers;

/// <summary>
/// Read and search of EHR data as FHIR:
///   GET /fhir/r4/{type}/{id}   read one resource        e.g. /fhir/r4/Patient/pat-1
///   GET /fhir/r4/{type}?...    search, returns a Bundle  e.g. /fhir/r4/Coverage?patient=pat-1&amp;status=active
/// Everything is fetched live from the EHR and converted; nothing is stored.
/// Callers: payers during CRD, payer DTR apps and the Payer Gateway. No authentication is built in.
/// The same endpoints answer under /fhir/r5 with the resources converted to FHIR R5 (Versioning/R5Converter).
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Route("fhir/r5")]
[Produces(FhirMediaTypes.FhirJson)]
[Tags("1. FHIR read and search - called by payers (CRD), payer DTR apps and the Payer Gateway")]
public class FhirResourceController : ControllerBase
{
    private readonly FhirReadService _reader;
    private readonly FhirSearchService _searcher;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public FhirResourceController(FhirReadService reader, FhirSearchService searcher, IOptions<FhirServerSettings> server)
    {
        _reader = reader;
        _searcher = searcher;
        _server = server.Value;
    }

    /// <summary>
    /// Reads one resource by type and id.
    /// Types: Patient, Coverage, Practitioner, PractitionerRole (id "role-{practitionerId}"), Organization, Location,
    /// ServiceRequest, DeviceRequest, MedicationRequest, Appointment, Encounter, DocumentReference, Binary.
    /// </summary>
    /// <param name="type" example="Patient">Required. FHIR resource type, case sensitive (e.g. "Patient").</param>
    /// <param name="id" example="pat-1">Required. Resource id (the EHR id), e.g. pat-1, cov-1, ord-1-hd, doc-1-labs.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The resource as FHIR JSON (content type application/fhir+json), e.g. { "resourceType": "Patient", "id": "pat-1", ... }.</response>
    /// <response code="400">The resource type is not supported. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "not-supported", "diagnostics": "Resource type Foo is not supported." } ] }.</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "not-found", "diagnostics": "Patient/pat-9 was not found." } ] }.</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("{type}/{id}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Read(string type, string id, CancellationToken ct)
    {
        var resource = await _reader.ReadAsync(type, id, ct)
                       ?? throw new FhirException(404, IssueTypes.NotFound, $"{type}/{id} was not found.");

        return FhirResults.Ok(resource, FhirRelease.FromRequest(Request), _server);
    }

    /// <summary>
    /// Searches one resource type and returns a "searchset" Bundle.
    /// Required: _id for Patient, Practitioner, PractitionerRole, Organization, Location, the order types and Appointment;
    /// patient for Coverage, Observation, Condition, Procedure, DocumentReference; _id or patient for Encounter.
    /// </summary>
    /// <param name="type" example="Coverage">Required. FHIR resource type, case sensitive (e.g. "Coverage").</param>
    /// <param name="search">Query parameters (see FhirSearchParameters). Fill only the ones the resource type uses.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type "searchset" (content type application/fhir+json): total = number of matches, entry = the resources found plus the ones added by _include. Nothing found = total 0 and no entry (still 200).</response>
    /// <response code="400">The resource type is not supported, or the parameter this type needs (_id or patient) is missing. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "required", "diagnostics": "Coverage search requires the patient parameter, e.g. Coverage?patient=pat-1." } ] }.</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("{type}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Search(string type, [FromQuery] FhirSearchParameters search, CancellationToken ct)
    {
        var bundle = await _searcher.SearchAsync(type, search, ct);

        return FhirResults.Ok(bundle, FhirRelease.FromRequest(Request), _server);
    }
}
