using Swashbuckle.AspNetCore.SwaggerUI;

namespace FHIRAPI.Helpers;

/// <summary>
/// Swagger UI options, and the "Schema" tab opened first so every property of a request body shows its description,
/// required mark and default value.
/// Sections are sorted alphabetically by title ("tagsSorter": "alpha"). Each FHIR resource type is its own section,
/// titled "FHIR APIs - Patient", "FHIR APIs - Coverage", ... (Swagger UI has no nested sections).
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
    }
}
