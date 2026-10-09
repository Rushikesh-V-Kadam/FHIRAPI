using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>Document metadata and file content (read by the FHIR API). Add documents with POST /api/patients/{id}/documents.</summary>
[ApiController]
[Route("api/documents")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class DocumentsController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public DocumentsController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>One clinical document (the description, not the file).</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR DocumentReference (GET /fhir/r4/DocumentReference/{id}).
    /// </remarks>
    /// <param name="id" example="doc-1-labs">EHR document id, e.g. doc-1-labs.</param>
    /// <response code="200">The document.</response>
    /// <response code="404">No document with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrDocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrDocumentDto> Get(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrDocumentDto document in _store.Data.Documents)
            {
                if (document.DocumentId == id)
                {
                    return Ok(document);
                }
            }
        }
        return NotFound(new ErrorResponse("Document " + id + " not found."));
    }

    /// <summary>The file of a clinical document, as Base64.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Binary (GET /fhir/r4/Binary/{id}). The gateway puts the file inside the
    /// prior authorization request (PAS) or the attachment (CDex) it sends to the payer, so files of several MB must work.
    /// </remarks>
    /// <param name="id" example="doc-1-labs">EHR document id, e.g. doc-1-labs.</param>
    /// <response code="200">The file.</response>
    /// <response code="404">No file is stored for this document.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}/content")]
    [ProducesResponseType(typeof(EhrDocumentContentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrDocumentContentDto> Content(string id)
    {
        lock (_store.Sync)
        {
            foreach (EhrDocumentContentDto content in _store.Data.DocumentContent)
            {
                if (content.DocumentId == id)
                {
                    return Ok(content);
                }
            }
        }
        return NotFound(new ErrorResponse("Content of document " + id + " not found."));
    }
}
