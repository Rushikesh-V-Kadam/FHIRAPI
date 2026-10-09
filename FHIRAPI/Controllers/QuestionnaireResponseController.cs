using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Services;
using FHIRAPI.Versioning;

namespace FHIRAPI.Controllers;

/// <summary>
/// DTR forms (FHIR QuestionnaireResponse), stored by this API in the EHR database (table PA_QUESTIONNAIRE_RESPONSE) and copied to the EHR chart:
///   POST /fhir/r4/QuestionnaireResponse        create (server assigns the id)
///   PUT  /fhir/r4/QuestionnaireResponse/{id}   create or update with a known id
///   GET  /fhir/r4/QuestionnaireResponse/{id}   read one form
///   GET  /fhir/r4/QuestionnaireResponse?patient=pat-1   all forms of a patient
/// Callers: the DTR app saves forms; the Payer Gateway reads them for PAS and CDex.
/// The same endpoints work under /fhir/r5. The QuestionnaireResponse elements DTR uses are the same in R4 and R5,
/// so a form is stored once, as received, and can be read through either release.
/// </summary>
[ApiController]
[Route("fhir/r4/QuestionnaireResponse")]
[Route("fhir/r5/QuestionnaireResponse")]
[Produces(FhirMediaTypes.FhirJson)]
[Tags("2. DTR forms (QuestionnaireResponse) - saved by the Payer Gateway or a payer DTR app")]
public class QuestionnaireResponseController : ControllerBase
{
    private readonly QuestionnaireResponseService _service;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public QuestionnaireResponseController(QuestionnaireResponseService service, IOptions<FhirServerSettings> server)
    {
        _service = service;
        _server = server.Value;
    }

    /// <summary>Creates a new form. Any id in the body is replaced by a server id. Returns 201 + Location header.</summary>
    /// <remarks>
    /// Must be filled in the body: resourceType = "QuestionnaireResponse" and subject.reference = "Patient/{patient id}".
    /// The order and the coverage the form is for are read from the DTR "qr-context" extensions, when present.
    /// </remarks>
    /// <param name="body" example='{"resourceType":"QuestionnaireResponse","questionnaire":"http://example.org/fhir/Questionnaire/dialysis-incenter-hd","status":"in-progress","subject":{"reference":"Patient/pat-1"},"authored":"2026-10-07T18:36:05Z","author":{"reference":"Practitioner/prac-1"},"extension":[{"url":"http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context","valueReference":{"reference":"ServiceRequest/ord-1-hd"}},{"url":"http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context","valueReference":{"reference":"Coverage/cov-1"}}],"item":[{"linkId":"egfr","text":"Most recent eGFR (mL/min/1.73 m2)","answer":[{"valueDecimal":9}]}]}'>Required. A FHIR QuestionnaireResponse (subject = Patient/{id} is required).</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="201">Saved. Body: the form as stored, with its new id; the Location header has its URL.</response>
    /// <response code="400">The body is not a QuestionnaireResponse, or subject does not point to a patient. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "required", "diagnostics": "QuestionnaireResponse.subject must reference a Patient, e.g. 'Patient/pat-1'." } ] }.</response>
    /// <response code="502">The EHR data API could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpPost]
    [Consumes(FhirMediaTypes.FhirJson, FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Create([FromBody, Required] JsonElement body, CancellationToken ct)
    {
        var (record, _) = await _service.SaveAsync(null, body, ct);
        var release = FhirRelease.FromRequest(Request);
        Response.Headers.Location = $"{release.BaseUrl(_server)}/QuestionnaireResponse/{record.Id}";
        return FhirResults.Ok(JsonNode.Parse(record.ResourceJson)!, release, _server, StatusCodes.Status201Created);
    }

    /// <summary>Creates (201) or replaces (200) the form with this id. The body id, if present, must equal the URL id.</summary>
    /// <remarks>
    /// Must be filled in the body: resourceType = "QuestionnaireResponse" and subject.reference = "Patient/{patient id}".
    /// Send the whole form each time: it replaces the stored one.
    /// </remarks>
    /// <param name="id" example="qr-frm-235b7ad69e85">Required. Form id chosen by the caller, e.g. "qr-frm-235b7ad69e85".</param>
    /// <param name="body" example='{"resourceType":"QuestionnaireResponse","questionnaire":"http://example.org/fhir/Questionnaire/dialysis-incenter-hd","status":"in-progress","subject":{"reference":"Patient/pat-1"},"authored":"2026-10-07T18:36:05Z","author":{"reference":"Practitioner/prac-1"},"extension":[{"url":"http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context","valueReference":{"reference":"ServiceRequest/ord-1-hd"}},{"url":"http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context","valueReference":{"reference":"Coverage/cov-1"}}],"item":[{"linkId":"egfr","text":"Most recent eGFR (mL/min/1.73 m2)","answer":[{"valueDecimal":9}]}]}'>Required. A FHIR QuestionnaireResponse.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">Replaced. Body: the form as stored.</response>
    /// <response code="201">Created (the id was new). Body: the form as stored.</response>
    /// <response code="400">The body is not a QuestionnaireResponse, or subject does not point to a patient, or the id in the body is not the id in the URL. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "required", "diagnostics": "QuestionnaireResponse.subject must reference a Patient, e.g. 'Patient/pat-1'." } ] }.</response>
    /// <response code="502">The EHR data API could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpPut("{id}")]
    [Consumes(FhirMediaTypes.FhirJson, FhirMediaTypes.Json)]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Update(string id, [FromBody, Required] JsonElement body, CancellationToken ct)
    {
        var (record, created) = await _service.SaveAsync(id, body, ct);
        var release = FhirRelease.FromRequest(Request);
        Response.Headers.Location = $"{release.BaseUrl(_server)}/QuestionnaireResponse/{record.Id}";
        return FhirResults.Ok(JsonNode.Parse(record.ResourceJson)!, release, _server, created ? StatusCodes.Status201Created : StatusCodes.Status200OK);
    }

    /// <summary>Returns one stored form exactly as it was saved.</summary>
    /// <param name="id" example="qr-frm-235b7ad69e85">Required. Form id.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">The FHIR QuestionnaireResponse (content type application/fhir+json).</response>
    /// <response code="404">No form with this id. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "not-found", "diagnostics": "QuestionnaireResponse/qr-9 was not found." } ] }.</response>
    /// <response code="502">The EHR data API could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Read(string id, CancellationToken ct)
    {
        var record = await _service.GetAsync(id, ct)
                     ?? throw new FhirException(404, IssueTypes.NotFound, $"QuestionnaireResponse/{id} was not found.");

        return FhirResults.Ok(JsonNode.Parse(record.ResourceJson)!, FhirRelease.FromRequest(Request), _server);
    }

    /// <summary>Returns all forms of a patient as a searchset Bundle (newest first).</summary>
    /// <param name="patient">Required. Patient id or "Patient/id", e.g. pat-1.</param>
    /// <param name="ct">Cancelled when the caller disconnects.</param>
    /// <response code="200">A FHIR Bundle of type "searchset" with the forms of the patient (total 0 and no entry when there are none).</response>
    /// <response code="400">The patient parameter is missing. Body: a FHIR OperationOutcome, e.g. { "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "required", "diagnostics": "QuestionnaireResponse search requires the patient parameter." } ] }.</response>
    /// <response code="502">The EHR data API could not be reached or answered with an error. Body: a FHIR OperationOutcome.</response>
    [HttpGet]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Search([FromQuery] string? patient, CancellationToken ct)
    {
        var patientId = patient?.Split('/').Last()
                        ?? throw new FhirException(400, IssueTypes.Required, "QuestionnaireResponse search requires the patient parameter.");

        return FhirResults.Ok(await _service.SearchByPatientAsync(patientId, ct), FhirRelease.FromRequest(Request), _server);
    }
}
