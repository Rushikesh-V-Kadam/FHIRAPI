using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// GET /fhir/r4/metadata - the CapabilityStatement.
///
/// Every FHIR server must publish one. It tells a client (payer, DTR app, Payer Gateway) which resource types
/// can be read and searched, which search parameters work, which writes are allowed (QuestionnaireResponse),
/// and where the SMART authorize/token endpoints are (this API's or Keycloak's). It is public: no token is needed to read it.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[Produces(FhirMediaTypes.FhirJson)]
[Tags("Metadata - CapabilityStatement (anyone)")]
public class MetadataController : ControllerBase
{
    /// <summary>Search parameters supported per type (anything not listed supports _id only).</summary>
    private static readonly Dictionary<string, string[]> SearchParameters = new()
    {
        ["Coverage"] = new[] { "patient", "status" },
        ["Encounter"] = new[] { "_id", "patient", "date" },
        ["Observation"] = new[] { "patient", "code", "category", "date" },
        ["Condition"] = new[] { "patient", "code", "clinical-status" },
        ["Procedure"] = new[] { "patient", "code", "date" },
        ["DocumentReference"] = new[] { "patient", "type" },
        ["QuestionnaireResponse"] = new[] { "patient" }
    };

    private readonly KeycloakSettings _keycloak;
    private readonly SecuritySettings _security;
    private readonly FhirServerSettings _server;

    /// <summary>Created by dependency injection.</summary>
    public MetadataController(IOptions<KeycloakSettings> keycloak, IOptions<SecuritySettings> security, IOptions<FhirServerSettings> server)
    {
        _keycloak = keycloak.Value;
        _security = security.Value;
        _server = server.Value;
    }

    /// <summary>Returns the CapabilityStatement of this FHIR server.</summary>
    /// <response code="200">A FHIR CapabilityStatement (content type application/fhir+json): the resource types, their search parameters and the SMART authorize / token URLs.</response>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK)]
    public IActionResult GetCapabilityStatement()
    {
        var resources = new List<object>();

        foreach (var type in FhirReadService.SupportedTypes)
            resources.Add(DescribeResource(type, canWrite: false));
        resources.Add(DescribeResource("QuestionnaireResponse", canWrite: true));

        var capabilityStatement = new
        {
            resourceType = "CapabilityStatement",
            status = "active",
            date = FhirDate.Now(),
            publisher = "FHIR API",
            kind = "instance",
            fhirVersion = FhirVersions.Fhir,
            format = new[] { "json" },
            implementationGuide = new[] { FhirVersions.UsCoreGuide },
            rest = new[]
            {
                new
                {
                    mode = "server",
                    security = new
                    {
                        service = new[]
                        {
                            new { coding = new[] { new { system = "http://terminology.hl7.org/CodeSystem/restful-security-service", code = "SMART-on-FHIR" } } }
                        },
                        extension = new[]
                        {
                            new
                            {
                                url = "http://fhir-registry.smarthealthit.org/StructureDefinition/oauth-uris",
                                extension = new[]
                                {
                                    new { url = "authorize", valueUri = AuthorizeUri() },
                                    new { url = "token", valueUri = TokenUri() }
                                }
                            }
                        }
                    },
                    resource = resources
                }
            }
        };

        return FhirResults.Ok(capabilityStatement);
    }

    /// <summary>The SMART authorize endpoint: this API's (BuiltIn) or Keycloak's.</summary>
    private string AuthorizeUri()
    {
        if (_security.UsesKeycloak())
        {
            return _keycloak.GetAuthorizationEndpoint();
        }
        return _server.BaseUrl + "/auth/authorize";
    }

    /// <summary>The SMART token endpoint: this API's (BuiltIn) or Keycloak's.</summary>
    private string TokenUri()
    {
        if (_security.UsesKeycloak())
        {
            return _keycloak.GetTokenEndpoint();
        }
        return _server.BaseUrl + "/auth/token";
    }

    /// <summary>One "rest.resource" entry: type, allowed interactions, search parameters.</summary>
    private static object DescribeResource(string type, bool canWrite)
    {
        var interactions = new List<object> { new { code = "read" }, new { code = "search-type" } };
        if (canWrite)
        {
            interactions.Add(new { code = "create" });
            interactions.Add(new { code = "update" });
        }

        var names = SearchParameters.TryGetValue(type, out var list) ? list : new[] { "_id" };
        var searchParams = names.Select(name => new
        {
            name,
            type = name switch { "date" => "date", "patient" => "reference", _ => "token" }
        });

        return new { type, interaction = interactions, searchParam = searchParams, searchInclude = new[] { "*" } };
    }
}
