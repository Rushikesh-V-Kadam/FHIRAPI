using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FHIRAPI.Helpers;

/// <summary>
/// Adds the "Authorize" button to Swagger UI, so calls made from Swagger send the same credentials as real callers
/// (Middleware/AccessTokenMiddleware.cs):
///   ApiKey  X-Api-Key header = Security:InternalApiKey (the Payer Gateway's key; full access)
///   Bearer  Authorization: Bearer {token} from Security:TokenProvider: this API (auth/token, /internal/access-tokens) or Keycloak
/// Either one is enough. Public endpoints (metadata, smart-configuration, authorize, token) ignore them.
/// </summary>
public static class SwaggerSecuritySetup
{
    private const string ApiKeyScheme = "ApiKey";
    private const string BearerScheme = "Bearer";

    /// <summary>Called inside AddSwaggerGen in Program.cs.</summary>
    public static void Configure(SwaggerGenOptions options)
    {
        OpenApiSecurityScheme apiKey = new OpenApiSecurityScheme();
        apiKey.Type = SecuritySchemeType.ApiKey;
        apiKey.In = ParameterLocation.Header;
        apiKey.Name = "X-Api-Key";
        apiKey.Description = "The Payer Gateway's key (Security:InternalApiKey). Local value: local-test-internal-key.";
        options.AddSecurityDefinition(ApiKeyScheme, apiKey);

        OpenApiSecurityScheme bearer = new OpenApiSecurityScheme();
        bearer.Type = SecuritySchemeType.Http;
        bearer.Scheme = "bearer";
        bearer.Description = "access_token from POST /fhir/r4/auth/token or POST /internal/access-tokens (BuiltIn), or from Keycloak. A token with a patient reaches that patient only.";
        options.AddSecurityDefinition(BearerScheme, bearer);

        // two separate requirements = two alternatives: the API key OR a Bearer token
        options.AddSecurityRequirement(ApiKeyRequirement);
        options.AddSecurityRequirement(BearerRequirement);
    }

    /// <summary>Requirement: the X-Api-Key header.</summary>
    private static OpenApiSecurityRequirement ApiKeyRequirement(OpenApiDocument document)
    {
        OpenApiSecurityRequirement requirement = new OpenApiSecurityRequirement();
        requirement[new OpenApiSecuritySchemeReference(ApiKeyScheme, document)] = new List<string>();
        return requirement;
    }

    /// <summary>Requirement: a Bearer token.</summary>
    private static OpenApiSecurityRequirement BearerRequirement(OpenApiDocument document)
    {
        OpenApiSecurityRequirement requirement = new OpenApiSecurityRequirement();
        requirement[new OpenApiSecuritySchemeReference(BearerScheme, document)] = new List<string>();
        return requirement;
    }
}
