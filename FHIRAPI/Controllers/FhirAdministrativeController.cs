using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Constants;
using FHIRAPI.Models.Requests;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// FHIR R4 read and search of Patient, insurance, practitioners, organizations and locations (facilities).
/// One action per resource type (GET /fhir/r4/{Type}/{id} and GET /fhir/r4/{Type}?...), so each resource has
/// its own URL and its own search parameters in Swagger. Everything is fetched live from the EHR and converted;
/// nothing is stored. Callers: payers during CRD, payer DTR apps and the Payer Gateway. No authentication is built in.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Produces(FhirMediaTypes.FhirJson)]
public class FhirAdministrativeController : FhirResourceControllerBase
{
    /// <summary>Created by dependency injection.</summary>
    public FhirAdministrativeController(FhirReadService reader, FhirSearchService searcher)
        : base(reader, searcher)
    {
    }

    /// <summary>Reads one Patient by id. GET /fhir/r4/Patient/{id}</summary>
    /// <param name="id" example="pat-1">Required. EHR patient id.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Patient as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Patient/{id}")]
    [Tags("FHIR APIs - Patient")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadPatient(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Patient", id, ct);
    }

    /// <summary>Searches Patient and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/Patient?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Patient:parameter[:TargetType], e.g. Patient:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Patient")]
    [Tags("FHIR APIs - Patient")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchPatient([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Patient", search, ct);
    }

    /// <summary>Reads one Coverage by id. GET /fhir/r4/Coverage/{id}</summary>
    /// <param name="id" example="cov-1">Required. The EHR's coverage id (unique in the EHR), e.g. the coverageId the EHR sends to the Payer Gateway. The FHIR API reads it with GET api/insurances/{coverageId}.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Coverage as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Coverage/{id}")]
    [Tags("FHIR APIs - Coverage")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadCoverage(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Coverage", id, ct);
    }

    /// <summary>Searches Coverage and returns a "searchset" Bundle. Required: _id, or patient (or beneficiary). GET /fhir/r4/Coverage?...</summary>
    /// <param name="id" example="cov-1">Required, or send patient. The EHR's coverage id(s), comma separated; each is read with GET api/insurances/{coverageId}.</param>
    /// <param name="patient" example="pat-1">Required, or send _id (or beneficiary). Patient id, or "Patient/{id}".</param>
    /// <param name="beneficiary">Optional. Same as patient (Coverage searches may send beneficiary).</param>
    /// <param name="status" example="active">Optional. Insurance status, e.g. active. Empty = every insurance of the patient.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Coverage:parameter[:TargetType], e.g. Coverage:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Coverage")]
    [Tags("FHIR APIs - Coverage")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchCoverage([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "patient")] string? patient, [FromQuery(Name = "beneficiary")] string? beneficiary, [FromQuery(Name = "status")] string? status, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Patient = patient;
        search.Beneficiary = beneficiary;
        search.Status = status;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Coverage", search, ct);
    }

    /// <summary>Reads one Practitioner by id. GET /fhir/r4/Practitioner/{id}</summary>
    /// <param name="id" example="prac-1">Required. EHR practitioner id. The client's EHR uses the practitioner's NPI as the id.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Practitioner as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Practitioner/{id}")]
    [Tags("FHIR APIs - Practitioner")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadPractitioner(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Practitioner", id, ct);
    }

    /// <summary>Searches Practitioner and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/Practitioner?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Practitioner:parameter[:TargetType], e.g. Practitioner:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Practitioner")]
    [Tags("FHIR APIs - Practitioner")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchPractitioner([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Practitioner", search, ct);
    }

    /// <summary>Reads one PractitionerRole by id. GET /fhir/r4/PractitionerRole/{id}</summary>
    /// <param name="id" example="role-prac-1">Required. role-{practitionerId}.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The PractitionerRole as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("PractitionerRole/{id}")]
    [Tags("FHIR APIs - PractitionerRole")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadPractitionerRole(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("PractitionerRole", id, ct);
    }

    /// <summary>Searches PractitionerRole and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/PractitionerRole?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as PractitionerRole:parameter[:TargetType], e.g. PractitionerRole:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("PractitionerRole")]
    [Tags("FHIR APIs - PractitionerRole")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchPractitionerRole([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("PractitionerRole", search, ct);
    }

    /// <summary>Reads one Organization by id. GET /fhir/r4/Organization/{id}</summary>
    /// <param name="id" example="org-1">Required. EHR organization id, or facility-{locationId} for a facility (a location with its own NPI).</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Organization as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Organization/{id}")]
    [Tags("FHIR APIs - Organization")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadOrganization(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Organization", id, ct);
    }

    /// <summary>Searches Organization and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/Organization?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Organization:parameter[:TargetType], e.g. Organization:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Organization")]
    [Tags("FHIR APIs - Organization")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchOrganization([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Organization", search, ct);
    }

    /// <summary>Reads one Location by id. GET /fhir/r4/Location/{id}</summary>
    /// <param name="id" example="loc-main">Required. EHR location (facility) id.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The Location as FHIR JSON (application/fhir+json).</response>
    /// <response code="404">The EHR has no such record. Body: a FHIR OperationOutcome (code not-found).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Location/{id}")]
    [Tags("FHIR APIs - Location")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> ReadLocation(string id, CancellationToken ct)
    {
        return await ReadResourceAsync("Location", id, ct);
    }

    /// <summary>Searches Location and returns a "searchset" Bundle. Required: _id. GET /fhir/r4/Location?...</summary>
    /// <param name="id">Required. One or more ids, comma separated.</param>
    /// <param name="include">Optional, repeatable. Related resources to add to the Bundle, as Location:parameter[:TargetType], e.g. Location:patient.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type searchset (application/fhir+json): total and the resources found, plus the _include ones. Nothing found = total 0 (still 200).</response>
    /// <response code="400">The required search parameter is missing. Body: a FHIR OperationOutcome (code required).</response>
    /// <response code="502">The EHR could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("Location")]
    [Tags("FHIR APIs - Location")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> SearchLocation([FromQuery(Name = "_id")] string? id, [FromQuery(Name = "_include")] List<string>? include,
        CancellationToken ct)
    {
        FhirSearchParameters search = new FhirSearchParameters();
        search.Id = id;
        search.Include = IncludeList(include);
        return await SearchResourcesAsync("Location", search, ct);
    }
}
