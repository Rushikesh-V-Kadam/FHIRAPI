using Swashbuckle.AspNetCore.SwaggerUI;

namespace FHIRAPI.Helpers;

/// <summary>
/// Swagger UI options: the sections in their numbered order (0 to 4), and the "Schema" tab opened first so every
/// property of a request body shows its description, required mark and default value.
/// Section 1 "FHIR APIs" has one sub-section per FHIR resource type. Swagger UI has no nested sections, so the
/// endpoints are tagged "1. FHIR APIs - Patient", ... and wwwroot/swagger-ui/fhir-sections.js + .css show them
/// under one "1. FHIR APIs" heading with only the resource type as the sub-section title.
/// </summary>
public static class SwaggerUiSetup
{
    /// <summary>Called by app.UseSwaggerUI in Program.cs.</summary>
    public static void Configure(SwaggerUIOptions options)
    {
        options.DocumentTitle = "FHIR API";
        options.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
        options.DefaultModelRendering(ModelRendering.Model);
        options.DefaultModelExpandDepth(2);
        // relative paths so they also work when the API runs under a virtual directory
        options.InjectStylesheet("../swagger-ui/fhir-sections.css");
        options.InjectJavascript("../swagger-ui/fhir-sections.js");
    }
}
