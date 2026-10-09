// =====================================================================================================
// Mock provider EHR (http://localhost:5090, Swagger UI at /swagger)
//
// Stands in for the client's EHR REST APIs:
//   - the read APIs the FHIR API converts to FHIR (patients, insurance, orders, clinical data, documents)
//   - the EHR data API (api/prior-auth-data): the Payer Gateway and the FHIR API keep all their data
//     in the EHR database and read / save it through these endpoints (PriorAuthDataController)
//   - the chart copy of DTR questionnaire responses written by the FHIR API
//   - add / edit APIs used by the React EHR app (mock-ehr-ui)
// All data is kept in one JSON file (MockEhr:DataFile, default Data/ehr-data.json), created from SeedData on first start.
// Controllers only; registrations are in Startup.cs.
// =====================================================================================================

using MockEhr.Api;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

Startup startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

WebApplication app = builder.Build();
startup.Configure(app);

app.Run();
