// =====================================================================================================
// FHIR API  (http://localhost:5100)
//
// Turns the EHR's plain REST/JSON APIs into a FHIR R4 server (US Core 3.1.1 / Da Vinci CRD profiles).
// Only DTR forms (QuestionnaireResponse) are stored by this API - in the EHR's Oracle database, through the EHR data API
// (table PA_QUESTIONNAIRE_RESPONSE). Every other read goes live to the EHR. This API has no database connection of its own.
//
//   /fhir/r4/metadata                          CapabilityStatement (public)
//   /fhir/r4/{type}/{id}, /fhir/r4/{type}      read / search EHR data as FHIR
//   /fhir/r4/QuestionnaireResponse[/{id}]      save / read DTR forms
//   /fhir/r4/.well-known/smart-configuration   SMART discovery (public)
//   /fhir/r4/auth/authorize, /fhir/r4/auth/token   SMART launch context for DTR apps (ours for demos, the payers' own apps)
//   /internal/access-tokens, /internal/smart-launches   Payer Gateway only (keep on the internal network)
//
// No authentication is built in: it is added outside this code.
//
// Every /fhir/r4 endpoint above also exists under /fhir/r5: the same data, converted to FHIR R5 on the way out
// (Versioning/R5Converter). R4 stays the main API: the Da Vinci guides, CMS-0057-F and the Payer Gateway use R4.
//
// Who calls it: payers (during CRD), payers' SMART DTR apps, and the Payer Gateway.
// This file only wires things together; each class has its own file and comments.
// =====================================================================================================

using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using FHIRAPI.Configuration;
using FHIRAPI.Helpers;
using FHIRAPI.Constants;
using FHIRAPI.Mapping;
using FHIRAPI.Middleware;
using FHIRAPI.Repositories.Database;
using FHIRAPI.Repositories.InMemory;
using FHIRAPI.Repositories.Interfaces;
using FHIRAPI.Services;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// ---------------------------------------------------------------- 1. settings (appsettings.json sections)
builder.Services.Configure<EhrSettings>(configuration.GetSection(EhrSettings.SectionName));
builder.Services.Configure<FhirServerSettings>(configuration.GetSection(FhirServerSettings.SectionName));
var mappingSettings = configuration.GetSection(FhirMappingSettings.SectionName).Get<FhirMappingSettings>() ?? new FhirMappingSettings();
builder.Services.AddSingleton(mappingSettings);
var storageSettings = configuration.GetSection(StorageSettings.SectionName).Get<StorageSettings>() ?? new StorageSettings();

// ---------------------------------------------------------------- 2. EHR REST client
builder.Services.AddTransient<CorrelationIdHandler>();
builder.Services.AddHttpClient<IEhrClient, EhrClient>((services, http) =>
    {
        var ehr = services.GetRequiredService<IOptions<EhrSettings>>().Value;
        http.BaseAddress = new Uri(ehr.BaseUrl.TrimEnd('/') + "/");
        http.Timeout = TimeSpan.FromSeconds(ehr.TimeoutSeconds);
    })
    .AddHttpMessageHandler<CorrelationIdHandler>();

// ---------------------------------------------------------------- 3. EHR -> FHIR mapping (stateless, one instance)
builder.Services.AddSingleton<CodeSystemMapper>();
builder.Services.AddSingleton<AdministrativeMapper>();
builder.Services.AddSingleton<OrderMapper>();
builder.Services.AddSingleton<ClinicalMapper>();
builder.Services.AddSingleton<QuestionnaireResponseMapper>();
builder.Services.AddSingleton<CoveragePatientIndex>();

// ---------------------------------------------------------------- 4. FHIR services (one instance per request)
builder.Services.AddScoped<FhirReadService>();
builder.Services.AddScoped<FhirSearchService>();
builder.Services.AddScoped<QuestionnaireResponseService>();

// ---------------------------------------------------------------- 5. QuestionnaireResponse storage: EHR database (data API) or memory
// Folders: Repositories\Interfaces (what the services use), Repositories\Database (EHR data API),
// Repositories\InMemory (local testing; data is lost when the API stops).
if (storageSettings.UseEhrApi)
    builder.Services.AddSingleton<IQuestionnaireResponseRepository, EhrApiQuestionnaireResponseRepository>();
else
    builder.Services.AddSingleton<IQuestionnaireResponseRepository, InMemoryQuestionnaireResponseRepository>();

// ---------------------------------------------------------------- 6. SMART launches (in memory, a few minutes each)
builder.Services.AddSingleton<ISmartLaunchStore, InMemorySmartLaunchStore>();
// fhirAuthorization tokens for payers (issued by /internal/access-tokens; not checked by this API)
builder.Services.AddSingleton<ITokenService, InMemoryTokenService>();

// ---------------------------------------------------------------- 7. MVC, errors, CORS, Swagger
builder.Services.AddControllers(options =>
{
    // Accept "Content-Type: application/fhir+json" bodies (QuestionnaireResponse POST/PUT)
    var jsonInput = options.InputFormatters.OfType<SystemTextJsonInputFormatter>().First();
    jsonInput.SupportedMediaTypes.Add(FhirMediaTypes.FhirJson);
});
builder.Services.AddExceptionHandler<FhirExceptionHandler>();   // errors -> OperationOutcome
builder.Services.AddProblemDetails();
// The DTR app runs in a browser on another origin, so CORS must allow it. Restrict origins in production.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FHIR API",
        Version = "v1",
        Description = "FHIR R4 and R5 view of the EHR. Sections show who calls each endpoint: payers during CRD, payer DTR apps, and the Payer Gateway (/internal)."
    });
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "FHIRAPI.xml");
    if (File.Exists(xmlFile)) options.IncludeXmlComments(xmlFile);
    // a property that can never be null (C# "string", not "string?") is not shown as "nullable"
    options.SupportNonNullableReferenceTypes();
});

var app = builder.Build();

// ---------------------------------------------------------------- request pipeline (order matters)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(SwaggerUiSetup.Configure);
app.UseCors();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
