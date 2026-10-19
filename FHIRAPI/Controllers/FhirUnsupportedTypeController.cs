using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// Answers a read or search of a resource type this FHIR API does not serve (e.g. GET /fhir/r4/Claim/1) with a FHIR
/// OperationOutcome, as FHIR clients expect, instead of an empty 404 from routing. Every supported type has its own
/// action in the per-resource controllers; the fixed routes there always win over these {type} routes.
/// Hidden from Swagger: it is an error answer, not an API.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Produces(FhirMediaTypes.FhirJson)]
[ApiExplorerSettings(IgnoreApi = true)]
public class FhirUnsupportedTypeController : ControllerBase
{
    /// <summary>GET /fhir/r4/{type}/{id} for a type that is not served: 400 not-supported.</summary>
    [HttpGet("{type}/{id}")]
    public IActionResult Read(string type, string id)
    {
        throw new FhirException(400, IssueTypes.NotSupported, "Read of " + type + " is not supported. Supported: "
            + string.Join(", ", FhirReadService.SupportedTypes) + ", Binary, QuestionnaireResponse.");
    }

    /// <summary>GET /fhir/r4/{type} for a type that is not served: 400 not-supported.</summary>
    [HttpGet("{type}")]
    public IActionResult Search(string type)
    {
        throw new FhirException(400, IssueTypes.NotSupported, "Search of " + type + " is not supported. Supported: "
            + "Patient, Coverage, Practitioner, PractitionerRole, Organization, Location, ServiceRequest, "
            + "MedicationRequest, Encounter, Observation, Condition, Procedure, DocumentReference, QuestionnaireResponse.");
    }
}
