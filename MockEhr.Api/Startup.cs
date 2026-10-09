using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Storage;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace MockEhr.Api;

/// <summary>Registers the store, controllers, CORS (for the React apps) and Swagger.</summary>
public class Startup
{
    private readonly IConfiguration _configuration;

    /// <summary>Receives appsettings.json.</summary>
    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>Services.</summary>
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<EhrStore>();
        services.AddScoped<ApiKeyFilter>();

        services.AddControllers(ConfigureMvc).AddJsonOptions(ConfigureJson);
        services.AddCors(ConfigureCors);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(ConfigureSwagger);
    }

    /// <summary>Request pipeline. The store is created at startup so the data file exists right away.</summary>
    public void Configure(WebApplication app)
    {
        app.Services.GetRequiredService<EhrStore>();
        app.UseSwagger();
        app.UseSwaggerUI(ConfigureSwaggerUi);
        app.UseCors();
        app.MapControllers();
    }

    /// <summary>Empty ids are allowed in POST bodies (the API creates them), so strings are not required automatically.</summary>
    private static void ConfigureMvc(MvcOptions options)
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    }

    /// <summary>JSON: camelCase (default) and no null values, same as the EHR contract.</summary>
    private static void ConfigureJson(JsonOptions options)
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    /// <summary>The React apps run on other ports (5173, 5174), so the browser needs CORS.</summary>
    private static void ConfigureCors(CorsOptions options)
    {
        CorsPolicyBuilder policy = new CorsPolicyBuilder();
        policy.AllowAnyOrigin();
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
        options.AddDefaultPolicy(policy.Build());
    }

    /// <summary>
    /// Swagger: two documents (pick one in Swagger UI with "Select a definition", top right).
    ///   /swagger/v1/swagger.json         every endpoint of the mock (sections 1 to 5)
    ///   /swagger/ehr-team/swagger.json   only what the real EHR must provide (sections 1 to 3) - the file to give to the EHR team
    /// Descriptions and example values come from the XML comments of the controllers and model classes.
    /// </summary>
    private static void ConfigureSwagger(SwaggerGenOptions options)
    {
        // ---------------------------------------------------------------- document 1: everything
        OpenApiInfo info = new OpenApiInfo();
        info.Title = "Mock EHR API";
        info.Version = "v1";
        info.Description = "Test stand-in for the client's EHR REST APIs. Data is stored in Data/ehr-data.json. " +
            "Sections show WHO calls each endpoint: 1 = Payer Gateway (saves its data), 2 = FHIR API (saves DTR forms), " +
            "3 = FHIR API (reads patient data and turns it into FHIR), 4 = Mock EHR UI (shows, adds and updates test data), " +
            "5 = old write-back endpoints nothing calls any more. An endpoint with two callers shows in both sections. " +
            "Sections 1 to 3 are what the client's real EHR must provide; 4 and 5 exist only in this mock. " +
            "For the EHR team there is a second document with sections 1 to 3 only: choose it under \"Select a definition\" " +
            "(file: /swagger/ehr-team/swagger.json).";
        options.SwaggerDoc(SwaggerDocuments.Everything, info);

        // ---------------------------------------------------------------- document 2: for the EHR team (sections 1 to 3)
        OpenApiInfo ehrTeam = new OpenApiInfo();
        ehrTeam.Title = "EHR API for prior authorization";
        ehrTeam.Version = "v1";
        ehrTeam.Description =
            "What the EHR provides so the Payer Gateway and the FHIR API can do prior authorization (Da Vinci CRD, DTR, PAS, CDex). " +
            "They have no database connection of their own: every read and every save goes through these endpoints.\n\n" +
            "**Sections (by who calls the endpoint)**\n" +
            "- **1. Data API - called by the Payer Gateway**: reads and saves the rows of the PA_* tables under `api/prior-auth-data`.\n" +
            "- **2. Data API - called by the FHIR API**: the filled-in DTR forms (table PA_QUESTIONNAIRE_RESPONSE).\n" +
            "- **3. EHR API - called by the FHIR API**: patient, insurance, orders, clinical data and documents in plain JSON; " +
            "the FHIR API turns them into FHIR for the payer.\n\n" +
            "**Rules for every endpoint**\n" +
            "- JSON, UTF-8, `Content-Type: application/json`, property names in camelCase.\n" +
            "- A property marked required (red star) is always filled. Any other property can be `null` or left out.\n" +
            "- Ids in the URL are URL-encoded by the caller; decode them before use.\n" +
            "- An error is any status other than 200 / 201 / 204. A short body `{ \"error\": \"...\" }` helps: the first 300 characters are logged.\n" +
            "- No authentication is built into the gateway; it is added outside this code. Use HTTPS.\n\n" +
            "**Extra rules for the data API (sections 1 and 2)**\n" +
            "- A row is a flat JSON object: one property = one column, camelCase = UPPER_SNAKE_CASE (`requestId` = `REQUEST_ID`).\n" +
            "- Properties ending in `Json` or `Bundle`, and `appContext`, are strings that hold JSON text. " +
            "Store the string unchanged in the CLOB column and return it unchanged (do not parse or re-format it).\n" +
            "- Timestamps are UTC, ISO-8601 with `Z`: `2026-10-07T18:36:17Z` (fractions of a second are allowed).\n" +
            "- Booleans are JSON `true` / `false` (stored as NUMBER(1) 1 / 0).\n" +
            "- PUT = insert, or update when the key in the URL already exists (Oracle MERGE). Answer 204 (or 200). " +
            "The key in the URL wins over the key in the body. A failed PUT is tried up to 3 times, so PUT must be safe to repeat.\n" +
            "- GET of one row = 200 with the row, or 404 when there is no such row. GET of a list = 200 with a JSON array (`[]` when nothing matches).\n" +
            "- Compare `payerName` without regard to upper / lower case wherever it is a key or a filter.\n" +
            "- You may return extra properties (your own audit columns); they are ignored.\n" +
            "- Allow request and response bodies of at least 20 MB (`lastRequestBundle` holds the attached clinical documents).\n\n" +
            "This document is produced by the mock EHR (MockEhr.Api), which implements every endpoint shown here and can be used to test against.";
        options.SwaggerDoc(SwaggerDocuments.EhrTeam, ehrTeam);

        options.DocInclusionPredicate(IsInDocument);
        options.DocumentFilter<EhrTeamDocumentFilter>();

        // descriptions and example values: the XML comments of the controllers and model classes
        string xmlFile = Path.Combine(AppContext.BaseDirectory, "MockEhr.Api.xml");
        if (File.Exists(xmlFile))
        {
            options.IncludeXmlComments(xmlFile);
        }

        // a property that can never be empty (C# "string", not "string?") is shown as required, with a red star
        options.SupportNonNullableReferenceTypes();
        options.NonNullableReferenceTypesAsRequired();
    }

    /// <summary>
    /// Decides which endpoints are in which Swagger document: "v1" has all of them, "ehr-team" only the endpoints
    /// of sections 1 to 3 (an endpoint is in a section through its [Tags(...)] attribute).
    /// </summary>
    private static bool IsInDocument(string documentName, ApiDescription endpoint)
    {
        if (documentName != SwaggerDocuments.EhrTeam)
        {
            return true;
        }
        foreach (object metadata in endpoint.ActionDescriptor.EndpointMetadata)
        {
            ITagsMetadata? sections = metadata as ITagsMetadata;
            if (sections == null)
            {
                continue;
            }
            foreach (string section in sections.Tags)
            {
                if (SwaggerGroups.IsForEhrTeam(section))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Swagger UI: both documents in the "Select a definition" list, the sections in their numbered order (1 to 5),
    /// and the "Schema" tab opened first so every property shows its description, required mark and default value.
    /// </summary>
    private static void ConfigureSwaggerUi(SwaggerUIOptions options)
    {
        options.DocumentTitle = "Mock EHR API";
        options.SwaggerEndpoint("/swagger/" + SwaggerDocuments.Everything + "/swagger.json", "Mock EHR API - everything (sections 1 to 5)");
        options.SwaggerEndpoint("/swagger/" + SwaggerDocuments.EhrTeam + "/swagger.json", "For the EHR team - what the real EHR must provide (sections 1 to 3)");
        options.ConfigObject.AdditionalItems["tagsSorter"] = "alpha";
        options.DefaultModelRendering(ModelRendering.Model);
        options.DefaultModelExpandDepth(2);
    }
}
