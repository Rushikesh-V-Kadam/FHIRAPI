# FHIR API - Postman tests

Files:
- `FHIRAPI.postman_collection.json` - 68 requests in 19 folders, each with test assertions
- `FHIRAPI-local.postman_environment.json` - URLs for a local run

## Before you run
1. Start the Mock EHR (http://localhost:5090) and the FHIR API (http://localhost:5100).
2. Reset the Mock EHR so the data matches what the tests expect:
   `POST http://localhost:5090/api/admin/reset`
   (the tests use the test data in `MockEhr.Api/Data/TestData`; data added by hand changes the totals).

## Import and run
1. Postman > Import > select both files.
2. Select the environment "FHIR API - local" (top right).
3. Run the whole collection with the Collection Runner, in the order shown.

Some requests save ids for the next ones, so run these folders in order:
- 2. Coverage: the first search saves `coverageId` (Coverage ids are built by the API, e.g. `cov-pat-1-3f9a...`).
- 15. QuestionnaireResponse: Create saves `qrId`, used by Update, Read and Search.
- 16. SMART launch: Create launch saves `launchId`, Authorize saves `authCode`, Token uses it.

From the command line (Newman):
`newman run FHIRAPI.postman_collection.json -e FHIRAPI-local.postman_environment.json`

## What is covered
Every endpoint of the FHIR API: metadata, read and search of Patient, Coverage, Practitioner, PractitionerRole,
Organization (parent and facility), Location, ServiceRequest, MedicationRequest, Encounter, Observation, Condition,
Procedure, DocumentReference, Binary; QuestionnaireResponse create / update / read / search; the SMART launch
(configuration, launch, authorize, token); the internal access token.
Also the error cases: not found (404), missing required search parameter (400), wrong resource type for an order (404),
used authorization code (400) and unsupported resource types (400).
