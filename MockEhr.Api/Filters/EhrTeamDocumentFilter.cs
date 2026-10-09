using Microsoft.OpenApi;
using MockEhr.Api.Constants;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MockEhr.Api.Filters;

/// <summary>
/// Tidies the Swagger document for the EHR team (/swagger/ehr-team/swagger.json).
/// Some endpoints have two callers and so two sections, e.g. GET api/patients/{id} is in "3. EHR API - called by FHIR API"
/// and in "4. Mock EHR UI". The EHR team document must not show the mock sections (4 and 5), so those section names
/// are taken off the endpoints here. Which endpoints are in the document is decided in Startup.IsInDocument.
/// </summary>
public class EhrTeamDocumentFilter : IDocumentFilter
{
    /// <summary>Called by Swagger for every document; only the EHR team document is changed.</summary>
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        if (context.DocumentName != SwaggerDocuments.EhrTeam)
        {
            return;
        }

        foreach (IOpenApiPathItem path in swaggerDoc.Paths.Values)
        {
            if (path.Operations == null)
            {
                continue;
            }
            foreach (OpenApiOperation operation in path.Operations.Values)
            {
                RemoveMockSections(operation);
            }
        }

        // the section list at the top of the document: only sections 1 to 3 are left
        if (swaggerDoc.Tags != null)
        {
            List<OpenApiTag> mockSections = new List<OpenApiTag>();
            foreach (OpenApiTag tag in swaggerDoc.Tags)
            {
                if (!SwaggerGroups.IsForEhrTeam(tag.Name))
                {
                    mockSections.Add(tag);
                }
            }
            foreach (OpenApiTag tag in mockSections)
            {
                swaggerDoc.Tags.Remove(tag);
            }
        }
    }

    /// <summary>Takes the mock sections (4 and 5) off one endpoint.</summary>
    private static void RemoveMockSections(OpenApiOperation operation)
    {
        if (operation.Tags == null)
        {
            return;
        }
        List<OpenApiTagReference> mockSections = new List<OpenApiTagReference>();
        foreach (OpenApiTagReference tag in operation.Tags)
        {
            if (!SwaggerGroups.IsForEhrTeam(tag.Name))
            {
                mockSections.Add(tag);
            }
        }
        foreach (OpenApiTagReference tag in mockSections)
        {
            operation.Tags.Remove(tag);
        }
    }
}
