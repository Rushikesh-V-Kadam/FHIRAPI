using Swashbuckle.AspNetCore.SwaggerUI;

namespace FHIRAPI.Helpers;

/// <summary>
/// Swagger UI options: the sections in their numbered order (0 to 4), and the "Schema" tab opened first so every
/// property of a request body shows its description, required mark and default value.
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
