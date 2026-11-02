// =====================================================================================================
// FHIR API  (http://localhost:5100)
//
// Turns the EHR's plain REST/JSON APIs into a FHIR R4 server (US Core 3.1.1 / Da Vinci CRD profiles).
// Only DTR forms (QuestionnaireResponse) are stored by this API - in the EHR's Oracle database, through the EHR data API
// (table PA_QUESTIONNAIRE_RESPONSE). Every other read goes live to the EHR. This API has no database connection of its own.
//
//   /fhir/r4/metadata                          CapabilityStatement (public)
//   /fhir/r4/Patient/{id}, /fhir/r4/Patient?... (one route per resource type) read / search EHR data as FHIR
//   /fhir/r4/QuestionnaireResponse[/{id}]      save / read DTR forms
//   /fhir/r4/.well-known/smart-configuration   SMART discovery (public): where DTR apps sign in
//   /fhir/r4/auth/authorize, /fhir/r4/auth/token   BuiltIn only: SMART sign-in and tokens issued by this API
//   /internal/access-tokens                    BuiltIn only: CRD fhirAuthorization token for the gateway
//   /internal/smart-launches[/{id}/context]    Payer Gateway (create a launch) and, with Keycloak, the Keycloak add-on
//                                              (read its context); keep on the internal network
//
// Who issues the tokens: Security:TokenProvider
//   "BuiltIn"   this API (tables PA_SMART_CLIENT, PA_SMART_AUTH_CODE, PA_ACCESS_TOKEN) - the default for now
//   "Keycloak"  Keycloak (appsettings "Keycloak"); clinicians sign in through Azure AD, brokered by Keycloak, and the
//               Keycloak SMART launch add-on (E:\FHIR\KeycloakSmartLaunch) puts the launch context next to the token
// Access checks (appsettings "Security", Middleware/AccessTokenMiddleware.cs):
//   /fhir/r4 needs a Bearer token from the TokenProvider, or the gateway's X-Api-Key; /internal needs the X-Api-Key. Security:ValidateAccessTokens = false turns the /fhir/r4 check off
//   (when it is done in front of this API).
//
// FHIR R4 only (4.0.1): the version the Da Vinci guides, CMS-0057-F and the Payer Gateway use.
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
builder.Services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
builder.Services.Configure<KeycloakSettings>(configuration.GetSection(KeycloakSettings.SectionName));
var securitySettings = configuration.GetSection(SecuritySettings.SectionName).Get<SecuritySettings>() ?? new SecuritySettings();
var keycloakSettings = configuration.GetSection(KeycloakSettings.SectionName).Get<KeycloakSettings>() ?? new KeycloakSettings();
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

// ---------------------------------------------------------------- 6. SMART apps, launches, codes and access tokens
// Storage:SmartStore = "EhrApi": tables PA_SMART_LAUNCH and (BuiltIn) PA_SMART_CLIENT, PA_SMART_AUTH_CODE, PA_ACCESS_TOKEN
// through the EHR data API (works with several server instances). "InMemory": one instance only; apps from
// Security:LocalClients. With Keycloak only the launches are used: apps, codes and tokens are kept by Keycloak.
if (storageSettings.UseEhrApiForSmart)
{
    builder.Services.AddSingleton<EhrDataApi>();
    builder.Services.AddSingleton<ISmartClientStore, EhrApiSmartClientStore>();
    builder.Services.AddSingleton<ISmartLaunchStore, EhrApiSmartLaunchStore>();
    builder.Services.AddSingleton<ITokenService, EhrApiTokenService>();
}
else
{
    builder.Services.AddSingleton<ISmartClientStore, InMemorySmartClientStore>();
    builder.Services.AddSingleton<ISmartLaunchStore, InMemorySmartLaunchStore>();
    builder.Services.AddSingleton<ITokenService, InMemoryTokenService>();
}
// Security:TokenProvider decides who checks the Bearer tokens on /fhir/r4
if (securitySettings.UsesKeycloak())
{
    // one instance: it caches Keycloak's public keys (Services/KeycloakTokenValidator.cs)
    builder.Services.AddSingleton<IAccessTokenValidator, KeycloakTokenValidator>();
}
else
{
    builder.Services.AddSingleton<IAccessTokenValidator, BuiltInTokenValidator>();
}

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
        Description = "FHIR R4 view of the EHR. Sections show who calls each endpoint: payers during CRD, payer DTR apps, and the Payer Gateway (/internal)."
    });
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "FHIRAPI.xml");
    if (File.Exists(xmlFile)) options.IncludeXmlComments(xmlFile);
    // a property that can never be null (C# "string", not "string?") is not shown as "nullable"
    options.SupportNonNullableReferenceTypes();
    // "Authorize" button: X-Api-Key or Bearer token (Helpers/SwaggerSecuritySetup.cs)
    SwaggerSecuritySetup.Configure(options);
});

var app = builder.Build();

// ---------------------------------------------------------------- request pipeline (order matters)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(SwaggerUiSetup.Configure);
app.UseCors();
app.UseMiddleware<AccessTokenMiddleware>();   // after CORS (preflight), before the controllers
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

if (securitySettings.ValidateAccessTokens && securitySettings.InternalApiKey.Length == 0)
{
    // without the key the Payer Gateway has no way in: every /fhir/r4 call would need a patient token
    app.Logger.LogWarning("Security:ValidateAccessTokens is on but Security:InternalApiKey is empty: the Payer Gateway cannot read the FHIR API.");
}
if (securitySettings.UsesKeycloak() && !keycloakSettings.IsConfigured())
{
    // without the realm no token can be checked and no app can sign in: those calls answer 503
    app.Logger.LogWarning("Security:TokenProvider is Keycloak but Keycloak:Authority is empty: SMART sign-in and Bearer tokens do not work.");
}

app.Run();
