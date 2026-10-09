# FHIR API: Endpoint Guide

**Project:** `FHIRAPI` (port 5100)
**Standards:** FHIR R4 4.0.1, US Core 3.1.1, Da Vinci CRD / DTR / PAS 2.0.1, CDex 2.0.0, SMART App Launch 2.x, CDS Hooks 2.0.
The same data is also served as **FHIR R5 5.0.0** under `/fhir/r5` (section 3.11); R4 stays the main API.
**Storage:** QuestionnaireResponse in the EHR's Oracle database (table `PA_QUESTIONNAIRE_RESPONSE`), read and saved through the EHR data API. This API has no database connection of its own. Everything else is read live from the EHR REST APIs and is not stored.

---

## 1. The big picture

The EHR only has plain REST/JSON APIs. The Da Vinci guides require FHIR. This API sits in front of the EHR and makes the EHR look like a FHIR server.

```
                                   +-------------------------------+
   EHR REST APIs  <--- reads ----  |   FHIR API (5100)    |  <--- FHIR reads ---  Payer (during CRD)
   (5090)         --- forms  --->  |                               |  <--- FHIR + SMART -- DTR app (browser)
                                   |  /fhir/r4/...   (FHIR R4)     |  <--- FHIR reads ---  Payer Gateway (5200)
                                   |  /fhir/r5/...   (FHIR R5 view)|  <--- R5 payers / R5 DTR apps (optional)
                                   |  /internal/...  (gateway only)|  <--- launches ---------- Payer Gateway
                                   +---------------+---------------+
                                                   |
                                                   v
                          EHR data API -> EHR Oracle: PA_QUESTIONNAIRE_RESPONSE
```

There are three callers, and each one uses a different set of endpoints.

| Caller | Why it calls | Endpoints it uses |
|---|---|---|
| **Payer Gateway** (our other solution) | Needs Patient, Coverage and orders as FHIR to build CRD, PAS and CDex calls | `/fhir/r4/{type}`, `/fhir/r4/{type}/{id}`, `/fhir/r4/QuestionnaireResponse/*`, `/internal/*` |
| **Payer** (e.g. the mock Da Vinci payer) | During CRD it may need more data than the prefetch contained | `/fhir/r4/{type}`, `/fhir/r4/{type}/{id}` |
| **DTR SMART app** (runs in the clinician's browser) | Pre-fills the payer's questionnaire from the chart and saves the completed form | `.well-known/smart-configuration`, `auth/authorize`, `auth/token`, `/fhir/r4/*`, `QuestionnaireResponse` |

**No authentication is built into this API.** No caller sends a key or token, and nothing is checked. Authentication is added outside this code (section 2).

### Where each endpoint fits in the prior-auth flow

| Flow step | What happens | FHIR API endpoints involved |
|---|---|---|
| 0. Discovery | A client asks what this server can do | `GET /fhir/r4/metadata`, `GET /fhir/r4/.well-known/smart-configuration` |
| 1. CRD (order-sign) | Gateway reads the order, patient and coverage, gets a fhirAuthorization token for the payer, and calls the payer's CDS service. The payer may read more data. | `GET /fhir/r4/{type}?_id=...&_include=...`, `GET /fhir/r4/Coverage?patient=...`, `POST /internal/access-tokens`, then payer calls `GET /fhir/r4/{type}/{id}` |
| 2. DTR | The user opens the DTR link. The gateway creates a launch, the DTR app gets its launch context, reads data and saves the form. | `POST /internal/smart-launches`, `GET /fhir/r4/auth/authorize`, `POST /fhir/r4/auth/token`, `GET /fhir/r4/Observation?...`, `PUT /fhir/r4/QuestionnaireResponse/{id}` |
| 3. PAS | The gateway builds the PAS Bundle and attaches the saved form | `GET /fhir/r4/{type}?_id=...`, `GET /fhir/r4/QuestionnaireResponse?patient=...` |
| 4. CDex | The gateway sends the documents and forms the payer asked for | `GET /fhir/r4/DocumentReference/{id}`, `GET /fhir/r4/Binary/{id}`, `GET /fhir/r4/QuestionnaireResponse/{id}` |

### Quick answers

| Endpoint | In one sentence | Required? |
|---|---|---|
| `GET /fhir/r4/metadata` | Tells clients which resources and searches this server supports | **Yes.** Every FHIR server must publish it |
| `GET /fhir/r4/{type}/{id}` | Returns one resource, e.g. `Patient/pat-1` | **Yes** |
| `GET /fhir/r4/{type}` | Search, e.g. `Coverage?patient=pat-1`. Returns a Bundle | **Yes.** See section 3.3 for why it seemed not to work |
| `POST /fhir/r4/QuestionnaireResponse` | Saves a new DTR form (server picks the id) | **Yes** (DTR 2.0.1 requires the app to be able to save forms) |
| `PUT /fhir/r4/QuestionnaireResponse/{id}` | Saves or updates a DTR form with a known id | **Yes** (the DTR app saves again as the user edits) |
| `POST /internal/access-tokens` | One-patient token the gateway sends to the payer as `fhirAuthorization` during CRD | **Recommended.** Not checked by this API until authentication is added |
| `POST /internal/smart-launches` | Creates the launch context before the DTR app opens | **Yes** if you use the SMART DTR app; not needed for EHR-rendered forms only |
| `GET /fhir/r4/.well-known/smart-configuration` | Tells the DTR app where to get its launch context | **Yes** for the SMART DTR app |
| `GET /fhir/r4/auth/authorize` | SMART step 1: issues a one-time code for the launch | **Yes** for the SMART DTR app |
| `POST /fhir/r4/auth/token` | SMART step 2: exchanges the code for the launch context | **Yes** for the SMART DTR app |

### Common rules

- **Base URL:** `http://localhost:5100/fhir/r4` locally. Set `FhirServer:PublicBaseUrl` for other environments.
- **FHIR content type:** responses are `application/fhir+json`. Request bodies may be `application/fhir+json` or `application/json`.
- **Errors:** every `/fhir/r4` error returns a FHIR `OperationOutcome`:

```json
{
  "resourceType": "OperationOutcome",
  "issue": [ { "severity": "error", "code": "required", "diagnostics": "Coverage search requires the patient parameter, e.g. Coverage?patient=pat-1." } ]
}
```

| HTTP | issue.code | When |
|---|---|---|
| 400 | `required` / `invalid` / `not-supported` | Missing search parameter, bad body, unsupported type |
| 404 | `not-found` | The EHR does not have that record |
| 502 | `exception` | The EHR API failed or timed out |
| 500 | `exception` | Unexpected error (the message includes the correlation id) |

- **Correlation id:** send `X-Correlation-Id` to trace one request across the EHR, this API and the gateway. It is returned in the response. If you don't send one, it is generated.

---

## 2. Authentication

**None is built in.** This API does not check keys, tokens, client ids, redirect URIs, PKCE or scopes, and it does not
limit a caller to one patient. Authentication (for example Azure AD / Entra ID in front of the API, or an API gateway) is
added outside this code.

The SMART endpoints (`.well-known/smart-configuration`, `auth/authorize`, `auth/token`) are still here because a SMART
DTR app gets its **launch context** through them: patient, encounter, user, orders, coverage and the payer's
`appContext`. They check nothing; the `access_token` they return is a random value no endpoint checks.

`POST /internal/access-tokens` still issues the short-lived, one-patient token the gateway sends to the payer as CDS Hooks
`fhirAuthorization` during CRD. The token is kept by `InMemoryTokenService`, but no endpoint checks it yet: when you add
authentication, that is the token to accept from payers.

Keep `/internal/*` on the internal network: only the Payer Gateway calls it.

---

## 3. Endpoints

### 3.1 `GET /fhir/r4/metadata`: CapabilityStatement

**Why we use it.** FHIR requires every server to publish a CapabilityStatement at `[base]/metadata`. It is the server's "menu": which resource types exist, which interactions work (read, search, create, update), which search parameters are supported, and where the SMART login endpoints are. Payers, the DTR app and test tools (Inferno, Touchstone, the HL7 validator) read it before they do anything else. Payers onboarding in 2027 will ask for it.

**Scenario.** A payer's CRD service is configured with our FHIR base URL. Before it runs queries, its FHIR client calls `/metadata` to check the FHIR version and that `Coverage` search by `patient` is supported. The DTR app may also read `rest.security` to find the OAuth URLs (it usually uses `smart-configuration` instead).

**Request**

```http
GET /fhir/r4/metadata
Accept: application/fhir+json
```

No parameters, no body, no authentication.

**Response 200** (shortened; one `resource` entry per supported type)

```json
{
  "resourceType": "CapabilityStatement",
  "status": "active",
  "date": "2026-09-28T17:05:00Z",
  "publisher": "FHIR API",
  "kind": "instance",
  "fhirVersion": "4.0.1",
  "format": [ "json" ],
  "implementationGuide": [ "http://hl7.org/fhir/us/core/ImplementationGuide/hl7.fhir.us.core|3.1.1" ],
  "rest": [ {
    "mode": "server",
    "security": {
      "service": [ { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/restful-security-service", "code": "SMART-on-FHIR" } ] } ],
      "extension": [ {
        "url": "http://fhir-registry.smarthealthit.org/StructureDefinition/oauth-uris",
        "extension": [
          { "url": "authorize", "valueUri": "http://localhost:5100/fhir/r4/auth/authorize" },
          { "url": "token", "valueUri": "http://localhost:5100/fhir/r4/auth/token" }
        ]
      } ]
    },
    "resource": [
      {
        "type": "Coverage",
        "interaction": [ { "code": "read" }, { "code": "search-type" } ],
        "searchParam": [ { "name": "patient", "type": "reference" }, { "name": "status", "type": "token" } ],
        "searchInclude": [ "*" ]
      },
      {
        "type": "QuestionnaireResponse",
        "interaction": [ { "code": "read" }, { "code": "search-type" }, { "code": "create" }, { "code": "update" } ],
        "searchParam": [ { "name": "patient", "type": "reference" } ],
        "searchInclude": [ "*" ]
      }
    ]
  } ]
}
```

**Code:** `Controllers/MetadataController.cs`

---

### 3.2 `GET /fhir/r4/{type}/{id}`: read one resource

**Why we use it.** This is the basic FHIR "read". Anyone who holds a reference such as `"Patient/pat-1"` or `"Coverage/cov-1"` fetches the resource with it. The API calls the matching EHR endpoint, converts the JSON to FHIR (US Core / CRD profile) and returns it.

**Scenarios**

1. **CRD, payer side.** The payer receives `order-sign` with a `DeviceRequest` that says `insurance: Coverage/cov-1`. If `Coverage` was not in the prefetch, the payer calls `GET /fhir/r4/Coverage/cov-1`.
2. **DTR app.** After login, the app reads the patient in context: `GET /fhir/r4/Patient/pat-1`.
3. **CDex.** The gateway reads `DocumentReference/doc-knee-xray`, then downloads the file from `Binary/doc-knee-xray` to attach it.

**Supported `{type}` values and the EHR call behind each**

| type | EHR call | Notes |
|---|---|---|
| Patient | `GET api/patients/{id}` | |
| Coverage | `GET api/patients/{patientId}/insurances` | The EHR has no "coverage by id" call, so the API must already know the patient. It learns this when it reads the order or searches `Coverage?patient=`. |
| Practitioner | `GET api/practitioners/{id}` | |
| PractitionerRole | `GET api/practitioners/{id}` | id is `role-{practitionerId}`, e.g. `role-prac-1` |
| Organization | `GET api/organizations/{id}` | |
| Location | `GET api/locations/{id}` | |
| ServiceRequest / DeviceRequest / MedicationRequest | `GET api/orders/{id}` | The EHR `orderType` decides the type. Asking `ServiceRequest/ord-cpap` for a device order returns 404. |
| Appointment | `GET api/appointments/{id}` | |
| Encounter | `GET api/encounters/{id}` | |
| DocumentReference | `GET api/documents/{id}` | `attachment.url` points to `Binary/{id}` |
| Binary | `GET api/documents/{id}/content` | The file, base64 encoded |
| QuestionnaireResponse | EHR data API (`api/prior-auth-data/questionnaire-responses`) | Served by `QuestionnaireResponseController` (section 3.5) |

**Request**

| Part | Value |
|---|---|
| Path `type` | Resource type, case-sensitive: `Patient`, not `patient` |
| Path `id` | EHR id |

```http
GET /fhir/r4/Patient/pat-1
Accept: application/fhir+json
```

**Response 200**

```json
{
  "resourceType": "Patient",
  "id": "pat-1",
  "meta": { "profile": [ "http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient" ] },
  "identifier": [ {
    "type": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/v2-0203", "code": "MR", "display": "Medical record number" } ] },
    "system": "http://provider.example.org/fhir/sid/mrn",
    "value": "MRN-000123"
  } ],
  "active": true,
  "name": [ { "use": "official", "family": "Rivera", "given": [ "Alex" ] } ],
  "telecom": [ { "system": "phone", "value": "214-555-0100", "use": "home" } ],
  "gender": "male",
  "birthDate": "1958-04-12",
  "address": [ { "line": [ "100 Demo Street" ], "city": "Dallas", "state": "TX", "postalCode": "75201", "country": "US" } ]
}
```

**Another example:** `GET /fhir/r4/DeviceRequest/ord-cpap`

```json
{
  "resourceType": "DeviceRequest",
  "id": "ord-cpap",
  "meta": { "profile": [ "http://hl7.org/fhir/us/davinci-crd/StructureDefinition/profile-devicerequest" ] },
  "status": "draft",
  "intent": "order",
  "priority": "routine",
  "codeCodeableConcept": { "coding": [ { "system": "https://bluebutton.cms.gov/resources/codesystem/hcpcs", "code": "E0601", "display": "CPAP device" } ] },
  "subject": { "reference": "Patient/pat-1" },
  "encounter": { "reference": "Encounter/enc-1" },
  "occurrenceDateTime": "2026-10-15",
  "authoredOn": "2026-10-01T10:00:00Z",
  "requester": { "reference": "Practitioner/prac-1" },
  "reasonCode": [ { "coding": [ { "system": "http://hl7.org/fhir/sid/icd-10-cm", "code": "G47.33", "display": "Obstructive sleep apnea" } ] } ],
  "insurance": [ { "reference": "Coverage/cov-1" } ]
}
```

**Errors**

```json
// 404: GET /fhir/r4/Patient/does-not-exist
{ "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "not-found", "diagnostics": "Patient/does-not-exist was not found." } ] }

// 403: payer token for pat-1 reading pat-2's data
{ "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "forbidden", "diagnostics": "The access token does not allow access to this patient." } ] }

// 400: GET /fhir/r4/Claim/1
{ "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "not-supported", "diagnostics": "Read of Claim is not supported. Supported: Patient, Coverage, ..." } ] }
```

**Code:** `Controllers/FhirResourceController.cs` → `Services/FhirReadService.cs` → `Mapping/*Mapper.cs`

---

### 3.3 `GET /fhir/r4/{type}`: search

**What it's for.** The FHIR "search" interaction returns a `Bundle` (type `searchset`) with every match. It is how clients find data when they don't know the id: "all active coverages of this patient", "BMI and SpO2 observations", "the order plus its requester and patient in one call".

**Scenarios**

1. **CRD prefetch.** The payer's discovery response contains prefetch templates such as `Coverage?patient={{context.patientId}}&status=active` and `DeviceRequest?_id={{context.draftOrders.DeviceRequest.id}}&_include=DeviceRequest:patient&_include=DeviceRequest:requester`. The gateway fills in the values and runs each template here. The results go into the hook's `prefetch`.
2. **DTR pre-population.** The payer's questionnaire has CQL that needs a BMI (LOINC 39156-5). The DTR app runs `GET /fhir/r4/Observation?patient=pat-1&code=http://loinc.org|39156-5`.
3. **PAS.** The gateway reads all the orders in one prior-auth request: `ServiceRequest?_id=ord-mri-lumbar,ord-mri-brain`.

#### Why it "was not working"

The old version had two problems:

1. **Swagger showed no query parameters.** The old action read `Request.Query` by hand, so Swagger had only the `type` box. Every search sent from Swagger arrived without `patient` or `_id`, and every type needs one of them, because the EHR can only look data up by patient or by id. So every Swagger call returned **400**: `"Coverage search requires the patient parameter"` or `"This search requires the _id parameter"`.
2. **The error was not obvious**, so it looked broken.

**The fix in this version:** the parameters are now declared (`Models/Requests/FhirSearchParameters.cs`), so Swagger shows a box for each one, and the error message gives a working example. A search with the right parameter works from Swagger, Postman or the `.http` file.

#### Required parameter per type

| type | Required | Optional |
|---|---|---|
| Patient, Practitioner, PractitionerRole, Organization, Location, ServiceRequest, DeviceRequest, MedicationRequest, Appointment | `_id` (comma-separated list allowed) | `_include` |
| Coverage | `patient` | `status` (e.g. `active`) |
| Encounter | `_id` **or** `patient` | `date=ge2026-01-01` |
| Observation | `patient` | `code`, `category`, `date=ge...` |
| Condition | `patient` | `code`, `clinical-status` |
| Procedure | `patient` | `code`, `date=ge...` |
| DocumentReference | `patient` | `type` (e.g. `http://loinc.org\|18748-4`) |
| QuestionnaireResponse | `patient` | (section 3.5) |

`patient` accepts `pat-1` or `Patient/pat-1`. `subject` and `beneficiary` are accepted as aliases.

#### Supported `_include` values

`{SourceType}:{parameter}[:{TargetType}]`

| Source | parameters |
|---|---|
| ServiceRequest | `patient`, `requester`, `performer`, `encounter`, `location`, `insurance` |
| DeviceRequest | `patient`, `requester`, `performer`, `encounter`, `insurance` |
| MedicationRequest | `patient`, `requester`, `encounter`, `intended-dispenser`, `insurance` |
| Encounter | `patient`, `practitioner`, `service-provider`, `location` |
| Appointment | `patient`, `practitioner`, `location` |
| Coverage | `patient` / `beneficiary` |
| PractitionerRole | `practitioner`, `organization` |

`...:requester:PractitionerRole` returns the practitioner's role (`role-prac-1`) plus the Practitioner, because the NPI is on the Practitioner.

**Request examples**

```http
### Coverage of a patient (CRD prefetch)
GET /fhir/r4/Coverage?patient=pat-1&status=active

### An order with its patient and requester (CRD prefetch)
GET /fhir/r4/DeviceRequest?_id=ord-cpap&_include=DeviceRequest:patient&_include=DeviceRequest:requester

### Two LOINC codes (DTR pre-population)
GET /fhir/r4/Observation?patient=pat-1&code=http://loinc.org|39156-5,http://loinc.org|59408-5

### X-ray reports (CDex)
GET /fhir/r4/DocumentReference?patient=pat-1&type=http://loinc.org|18748-4
```

**Response 200:** `GET /fhir/r4/Coverage?patient=pat-1&status=active`

```json
{
  "resourceType": "Bundle",
  "id": "5d7b0f7f3c6c4a9ba6d1a3a7f0c3e111",
  "type": "searchset",
  "timestamp": "2026-09-28T17:05:00Z",
  "total": 1,
  "link": [ { "relation": "self", "url": "http://localhost:5100/fhir/r4/Coverage" } ],
  "entry": [ {
    "fullUrl": "http://localhost:5100/fhir/r4/Coverage/cov-1",
    "resource": {
      "resourceType": "Coverage",
      "id": "cov-1",
      "meta": { "profile": [ "http://hl7.org/fhir/us/davinci-crd/StructureDefinition/profile-coverage" ] },
      "identifier": [ {
        "type": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/v2-0203", "code": "MB", "display": "Member Number" } ] },
        "value": "MBR-1001"
      } ],
      "status": "active",
      "type": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/v3-ActCode", "code": "HIP" } ] },
      "subscriberId": "MBR-1001",
      "beneficiary": { "reference": "Patient/pat-1" },
      "relationship": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/subscriber-relationship", "code": "self" } ] },
      "period": { "start": "2026-01-01", "end": "2027-12-31" },
      "payor": [ { "identifier": { "value": "PAYER001" }, "display": "Mock Da Vinci Payer" } ],
      "class": [
        { "type": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/coverage-class", "code": "plan" } ] }, "value": "GOLD-PPO", "name": "Gold PPO" },
        { "type": { "coding": [ { "system": "http://terminology.hl7.org/CodeSystem/coverage-class", "code": "group" } ] }, "value": "GRP-500" }
      ]
    },
    "search": { "mode": "match" }
  } ]
}
```

With `_include`, the Bundle has the match first, then the included resources with `"search": { "mode": "include" }`:

```json
"entry": [
  { "fullUrl": ".../DeviceRequest/ord-cpap", "resource": { "resourceType": "DeviceRequest", "id": "ord-cpap", "...": "..." }, "search": { "mode": "match" } },
  { "fullUrl": ".../Patient/pat-1",          "resource": { "resourceType": "Patient", "id": "pat-1", "...": "..." },          "search": { "mode": "include" } },
  { "fullUrl": ".../Practitioner/prac-1",    "resource": { "resourceType": "Practitioner", "id": "prac-1", "...": "..." },    "search": { "mode": "include" } }
]
```

**Response when nothing matches:** 200 with `"total": 0` and no `entry`. An empty search is not an error.

**Error 400 (missing parameter):**

```json
{ "resourceType": "OperationOutcome", "issue": [ { "severity": "error", "code": "required", "diagnostics": "Observation search requires the patient parameter, e.g. Observation?patient=pat-1." } ] }
```

**Code:** `Controllers/FhirResourceController.cs` → `Services/FhirSearchService.cs` (+ `ReferenceFinder.cs` for `_include`)

---

### 3.4 `POST /fhir/r4/QuestionnaireResponse`: save a new DTR form

**Why we use it.** Under DTR (Documentation Templates and Rules), the payer supplies a Questionnaire, and the DTR app fills in what it can from the chart and lets the clinician complete the rest. The result is a FHIR **QuestionnaireResponse**. DTR 2.0.1 requires that the completed (or partly completed) form is **saved in the provider's system**, for three reasons:

1. **PAS** attaches it to the prior-auth request (the payer uses it to decide).
2. **CDex** sends it again when the payer asks for more documentation.
3. **Records:** it is part of the medical record, and the clinician can reopen and finish an in-progress form.

`POST` creates a new form. The server assigns the id, and any id in the body is replaced. The DTR app uses `POST` the first time it saves when it has no id of its own.

**What happens when a form is saved**

```
DTR app ──POST/PUT──> QuestionnaireResponseController
                         │ 1. validate: resourceType, subject = Patient/{id}, token patient
                         │ 2. read qr-context → orderId, coverageId
                         │ 3. copy to the EHR chart (flattened, one row per answer) → EHR_SYNC_STATUS = SENT / FAILED
                         │ 4. PUT to the EHR data API → table PA_QUESTIONNAIRE_RESPONSE (full FHIR JSON in a CLOB)
                         └──> 201 Created + Location + the stored resource
```

If the chart copy (step 3) fails, the form is **still saved** with `EHR_SYNC_STATUS = 'FAILED'`, so nothing is lost. If step 4 fails, the caller gets `502` and should save again.

#### Where to store it: the EHR's Oracle database, through the EHR data API

The client decided that there is **one database: the EHR's**. This API does not connect to Oracle. It calls the same
Web API the Payer Gateway uses for its tables (the EHR data API, see the gateway's `docs/EHR-Data-API.md`):

| Call | Purpose |
|---|---|
| `GET  {Ehr:BaseUrl}/{Ehr:DataPath}/questionnaire-responses/{id}` | one form (`404` = not found) |
| `GET  {Ehr:BaseUrl}/{Ehr:DataPath}/questionnaire-responses?patientId=...` | all forms of a patient |
| `PUT  {Ehr:BaseUrl}/{Ehr:DataPath}/questionnaire-responses/{id}` | insert or update |

The row is `QuestionnaireResponseRecord` as camelCase JSON; every property is one column (`resourceJson` ↔ `RESOURCE_JSON`).
The complete FHIR JSON is kept as text in a CLOB because the payer must receive the form **unchanged** (PAS / CDex);
the EHR data API must return it exactly as it was saved. Code: `Repositories/Database/EhrApiQuestionnaireResponseRepository.cs`.

Table (created by the EHR team with the gateway's `PayerGatewayWebAPI/Database/01_create_tables.sql`):

| Column | Type | Content |
|---|---|---|
| `ID` (PK) | VARCHAR2(64) | QuestionnaireResponse.id |
| `PATIENT_ID` | VARCHAR2(64) | from `subject` |
| `ORDER_ID` | VARCHAR2(64) | from the `qr-context` extension (order) |
| `COVERAGE_ID` | VARCHAR2(64) | from the `qr-context` extension (Coverage) |
| `QUESTIONNAIRE_URL` | VARCHAR2(1000) | payer questionnaire canonical |
| `STATUS` | VARCHAR2(20) | in-progress / completed / amended / ... |
| `RESOURCE_JSON` | CLOB, `IS JSON` | the complete FHIR resource |
| `EHR_RESPONSE_ID` | VARCHAR2(64) | id of the EHR copy (for updates) |
| `EHR_SYNC_STATUS` | VARCHAR2(10) | PENDING / SENT / FAILED |
| `LAST_UPDATED_UTC` | TIMESTAMP (UTC) | |

Settings:

```json
"Storage": { "QuestionnaireResponseStore": "EhrApi" },
"Ehr": { "BaseUrl": "https://ehr-host", "DataPath": "api/prior-auth-data", "TimeoutSeconds": 30 }
```

`"QuestionnaireResponseStore": "InMemory"` keeps the forms in memory instead (local testing without the data API).

**Request**

| Part | Value |
|---|---|
| Headers | `Content-Type: application/fhir+json` (or `application/json`) |
| Body | A FHIR QuestionnaireResponse. Required: `resourceType`, `subject.reference = "Patient/{id}"`. Recommended (DTR): `questionnaire`, `status`, `qr-context` extensions for the order and the coverage, `author`, `authored`, `item`. |

```http
POST /fhir/r4/QuestionnaireResponse
Content-Type: application/fhir+json

{
  "resourceType": "QuestionnaireResponse",
  "meta": { "profile": [ "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/dtr-questionnaireresponse" ] },
  "extension": [
    { "url": "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context", "valueReference": { "reference": "DeviceRequest/ord-cpap" } },
    { "url": "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context", "valueReference": { "reference": "Coverage/cov-1" } }
  ],
  "questionnaire": "http://example.org/fhir/Questionnaire/cpap-e0601",
  "status": "in-progress",
  "subject": { "reference": "Patient/pat-1" },
  "authored": "2026-10-01T10:55:00Z",
  "author": { "reference": "Practitioner/prac-1" },
  "item": [
    { "linkId": "sleep-study", "item": [
      { "linkId": "sleep-study-date", "answer": [ { "valueDate": "2026-08-10" } ] },
      { "linkId": "ahi", "answer": [ {
          "valueDecimal": 32,
          "extension": [ { "url": "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/information-origin",
                           "extension": [ { "url": "source", "valueCode": "auto" } ] } ]
      } ] }
    ] },
    { "linkId": "face-to-face", "answer": [ { "valueBoolean": true } ] }
  ]
}
```

**Response 201 Created**

Header: `Location: http://localhost:5100/fhir/r4/QuestionnaireResponse/9f1c2e4b7a6d4f0b8e3c5a1d2b4c6e8f`

Body: the stored resource, which is your JSON plus the server `id` and `meta.lastUpdated`:

```json
{
  "resourceType": "QuestionnaireResponse",
  "meta": {
    "profile": [ "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/dtr-questionnaireresponse" ],
    "lastUpdated": "2026-10-01T10:55:02Z"
  },
  "extension": [ "... unchanged ..." ],
  "questionnaire": "http://example.org/fhir/Questionnaire/cpap-e0601",
  "status": "in-progress",
  "subject": { "reference": "Patient/pat-1" },
  "item": [ "... unchanged ..." ],
  "id": "9f1c2e4b7a6d4f0b8e3c5a1d2b4c6e8f"
}
```

(JSON property order is not significant in FHIR.)

What the EHR receives (`POST api/patients/pat-1/questionnaire-responses`), one row per answer:

```json
{
  "responseId": "",
  "patientId": "pat-1",
  "orderId": "ord-cpap",
  "coverageId": "cov-1",
  "questionnaireUrl": "http://example.org/fhir/Questionnaire/cpap-e0601",
  "status": "in-progress",
  "authoredDateTime": "2026-10-01T10:55:00Z",
  "authorPractitionerId": "prac-1",
  "answers": [
    { "linkId": "sleep-study-date", "valueType": "date", "value": "2026-08-10" },
    { "linkId": "ahi", "valueType": "decimal", "value": 32, "origin": "auto" },
    { "linkId": "face-to-face", "valueType": "boolean", "value": true }
  ],
  "fhirJson": "{ ...the complete FHIR JSON... }"
}
```

**Errors**

| HTTP | diagnostics |
|---|---|
| 400 | `resourceType must be "QuestionnaireResponse".` |
| 400 | `QuestionnaireResponse.subject must reference a Patient, e.g. "Patient/pat-1".` |
| 403 | `The access token does not allow writing forms for this patient.` |
| 415 | Wrong `Content-Type` (use `application/fhir+json` or `application/json`) |

**Code:** `Controllers/QuestionnaireResponseController.cs` → `Services/QuestionnaireResponseService.cs` → `Repositories/Database/EhrApiQuestionnaireResponseRepository.cs` and `Mapping/QuestionnaireResponseMapper.cs` (EHR copy)

---

### 3.5 `PUT /fhir/r4/QuestionnaireResponse/{id}` and `GET /fhir/r4/QuestionnaireResponse/{id}`

**Why we use PUT.** DTR apps save several times: first as `in-progress` while the clinician works, then `completed` when they submit. Each save must replace the same form. `PUT` with the form's id is FHIR's "update" (or "create with a client-chosen id"). The Payer Gateway's test script and most DTR apps use `PUT`.

- If the id is new, the form is created and the response is **201**.
- If the id exists, the form is replaced and the response is **200**. The EHR copy is updated too (the stored `EHR_RESPONSE_ID` is reused).
- If the body has an `id`, it must equal the URL id, otherwise **400**.

**Scenario.** The clinician opens the CPAP form at 10:50 and the app saves `qr-cpap-1` as `in-progress`. At 11:00 they tick the attestation and submit, and the app `PUT`s the same id with `status: completed`. Later the gateway builds the PAS request for `ord-cpap`, finds `qr-cpap-1` and attaches it.

**PUT request**

```http
PUT /fhir/r4/QuestionnaireResponse/qr-cpap-1
Content-Type: application/fhir+json

{
  "resourceType": "QuestionnaireResponse",
  "id": "qr-cpap-1",
  "extension": [
    { "url": "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context", "valueReference": { "reference": "DeviceRequest/ord-cpap" } },
    { "url": "http://hl7.org/fhir/us/davinci-dtr/StructureDefinition/qr-context", "valueReference": { "reference": "Coverage/cov-1" } }
  ],
  "questionnaire": "http://example.org/fhir/Questionnaire/cpap-e0601",
  "status": "completed",
  "subject": { "reference": "Patient/pat-1" },
  "authored": "2026-10-01T11:00:00Z",
  "author": { "reference": "Practitioner/prac-1" },
  "item": [
    { "linkId": "face-to-face", "answer": [ { "valueBoolean": true } ] },
    { "linkId": "attestation", "answer": [ { "valueBoolean": true } ] }
  ]
}
```

**PUT response:** `201 Created` the first time, then `200 OK`. The header is `Location: .../QuestionnaireResponse/qr-cpap-1` and the body is the stored form (with `meta.lastUpdated`).

**Why we use GET by id.** It reads a stored form back exactly as it was saved. The gateway calls it in **CDex** when the payer's Task asks for a completed form (`questionnaireResponseIds: ["qr-knee-1"]`). The DTR app calls it to reopen an in-progress form.

```http
GET /fhir/r4/QuestionnaireResponse/qr-cpap-1
```

**GET response 200:** the stored JSON. **404** if the id is unknown.

**Search by patient** (used by PAS to find the forms for the orders):

```http
GET /fhir/r4/QuestionnaireResponse?patient=pat-1
```

Returns a `searchset` Bundle, newest first.

---

### 3.7 `POST /internal/smart-launches`: start a DTR app session

**What it's for.** The DTR app is a **SMART on FHIR app**. SMART's "EHR launch" works like this: the EHR opens the app with two URL parameters, `iss` (the FHIR server) and `launch` (an opaque id). The app trades the `launch` for a token that carries the context: which patient, which orders, which questionnaires.

Our EHR can't launch SMART apps itself, so the Payer Gateway and this API do it for the EHR. This endpoint stores the context (patient, user, encounter, orders, coverage, the payer's `appContext`) and returns a `launchId`.

**When it's needed:** every time a user opens a DTR link from a CRD card. It isn't needed if you only use EHR-rendered forms (`POST /api/v1/pa/dtr/questionnaire-package` on the gateway).

**Scenario**

1. CRD returns "documentation needed" for `ord-cpap` with a DTR link (`/api/v1/pa/dtr/launch/{linkId}` on the gateway).
2. The clinician clicks it. The gateway calls this endpoint with `patientId=pat-1`, `fhirContext=["DeviceRequest/ord-cpap","Coverage/cov-1"]` and the card's `appContext`.
3. The gateway redirects the browser to `https://dtr.example.org/launch?iss=http://localhost:5100/fhir/r4&launch=Xq3...`.
4. The DTR app continues with sections 3.8 to 3.10.

**Request**

| Part | Value |
|---|---|
| `patientId` | Required |
| `userPractitionerId` | Logged-in clinician (becomes `fhirUser`) |
| `encounterId` | Current encounter |
| `fhirContext` | References the app should work on: orders and coverage |
| `appContext` | String from the payer's CRD card (which questionnaires, coverage-assertion-id) |

```http
POST /internal/smart-launches
Content-Type: application/json

{
  "patientId": "pat-1",
  "userPractitionerId": "prac-1",
  "encounterId": "enc-1",
  "fhirContext": [ "DeviceRequest/ord-cpap", "Coverage/cov-1" ],
  "appContext": "{\"questionnaire\":[\"http://example.org/fhir/Questionnaire/cpap-e0601\"],\"coverage-assertion-id\":\"ca-7781\"}"
}
```

**Response 200**

```json
{
  "launchId": "Xq3vT9bK2mWfP0aZrY7c",
  "iss": "http://localhost:5100/fhir/r4",
  "expiresAt": "2026-10-01T11:05:00+00:00"
}
```

The launch must be used within **15 minutes**.

**Code:** `Controllers/InternalController.cs`, `Services/InMemorySmartLaunchStore.cs`

---

### 3.8 `GET /fhir/r4/.well-known/smart-configuration`: SMART discovery

**What it's for.** A SMART app knows only `iss` (our FHIR base URL). The SMART App Launch spec says the app finds everything else at `{iss}/.well-known/smart-configuration`: the authorize URL, the token URL, supported scopes, PKCE methods and capabilities. Without it, a standard SMART DTR app can't start.

**Scenario.** The DTR app is opened with `iss=http://localhost:5100/fhir/r4&launch=Xq3...`. Its first call is this endpoint.

**Request**

```http
GET /fhir/r4/.well-known/smart-configuration
```

No parameters, no authentication.

**Response 200**

```json
{
  "issuer": "http://localhost:5100/fhir/r4",
  "authorization_endpoint": "http://localhost:5100/fhir/r4/auth/authorize",
  "token_endpoint": "http://localhost:5100/fhir/r4/auth/token",
  "grant_types_supported": [ "authorization_code" ],
  "token_endpoint_auth_methods_supported": [ "none" ],
  "code_challenge_methods_supported": [ "S256" ],
  "response_types_supported": [ "code" ],
  "scopes_supported": [ "launch", "openid", "fhirUser", "patient/*.rs", "user/*.rs", "patient/QuestionnaireResponse.cu" ],
  "capabilities": [ "launch-ehr", "client-public", "context-ehr-patient", "context-ehr-encounter", "permission-v2", "permission-patient", "permission-user" ]
}
```

**Code:** `Controllers/SmartAuthController.cs` → `GetConfiguration`

---

### 3.9 `GET /fhir/r4/auth/authorize`: SMART step 1 (get a code)

**Why we use it.** This is the standard SMART / OAuth 2.0 authorization endpoint. The DTR app sends the browser here with the `launch` id and its redirect URI. The API sends the browser back to the app with a **one-time code** (valid 2 minutes). Nothing else is checked (section 2).

There's no login page because the clinician is already signed in to the EHR. The launch id proves that the EHR (through the gateway) started this session.

**Scenario.** After discovery, the DTR app redirects the browser:

```
http://localhost:5100/fhir/r4/auth/authorize
  ?response_type=code
  &client_id=dtr-app
  &redirect_uri=https%3A%2F%2Fdtr.example.org%2Fcallback
  &launch=Xq3vT9bK2mWfP0aZrY7c
  &scope=launch%20openid%20fhirUser%20patient%2F*.rs%20patient%2FQuestionnaireResponse.cu
  &state=af0ifjsldkj
  &code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM
  &code_challenge_method=S256
```

**Query parameters**

| Name | Required | Notes |
|---|---|---|
| `response_type` | yes | must be `code` |
| `client_id` | sent by apps | not checked |
| `redirect_uri` | yes | where the code is sent |
| `launch` | yes | from `/internal/smart-launches` |
| `scope` | no | default `launch patient/*.rs patient/QuestionnaireResponse.cu` |
| `state` | recommended | returned unchanged |
| `aud`, `code_challenge`, `code_challenge_method` | sent by apps | ignored |

The same parameters can be sent as a form post (`POST auth/authorize`, SMART "authorize-post").

**Response: 302 Found** (success)

```
Location: https://dtr.example.org/callback?code=Lm8Qf2...&state=af0ifjsldkj
```

**Response: 302** (unknown or expired launch; the error goes to the app)

```
Location: https://dtr.example.org/callback?error=invalid_request&error_description=unknown%20or%20expired%20launch&state=af0ifjsldkj
```

**Response: 400** when `redirect_uri` is missing.

---

### 3.10 `POST /fhir/r4/auth/token`: SMART step 2 (get the launch context)

**Why we use it.** The DTR app exchanges the one-time code for the **SMART launch context**: the patient, the encounter, the user (`fhirUser`), the orders and coverage (`fhirContext`) and the payer's `appContext`. The `access_token` in the answer is a random value; no endpoint checks it.

**Scenario.** The browser lands on `https://dtr.example.org/callback?code=Lm8Qf2...&state=...`. The app checks `state` and posts the code.

**Request** (form-encoded, as OAuth requires)

```http
POST /fhir/r4/auth/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&code=Lm8Qf2...&redirect_uri=https%3A%2F%2Fdtr.example.org%2Fcallback
```

| Field | Required | Notes |
|---|---|---|
| `grant_type` | yes | `authorization_code` |
| `code` | yes | from step 1; single use |
| `redirect_uri` | sent by apps | not checked |
| `client_id`, `code_verifier`, client credentials | sent by apps | ignored |

**Response 200** (header `Cache-Control: no-store`)

```json
{
  "access_token": "7hYc2Kq9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "launch fhirUser patient/*.rs patient/QuestionnaireResponse.cu",
  "patient": "pat-1",
  "encounter": "enc-1",
  "fhirUser": "Practitioner/prac-1",
  "fhirContext": [ { "reference": "DeviceRequest/ord-cpap" }, { "reference": "Coverage/cov-1" } ],
  "appContext": "{\"questionnaire\":[\"http://example.org/fhir/Questionnaire/cpap-e0601\"],\"coverage-assertion-id\":\"ca-7781\"}",
  "need_patient_banner": false
}
```

The app then:

1. reads `Patient/pat-1`, `DeviceRequest/ord-cpap` and `Coverage/cov-1` here;
2. calls the gateway's `$questionnaire-package` to get the payer's Questionnaire and CQL;
3. runs the CQL against `/fhir/r4/Observation?...`, `/Condition?...` and so on to pre-fill;
4. saves with `PUT /fhir/r4/QuestionnaireResponse/{id}`.

**Errors (400)**

```json
{ "error": "invalid_grant", "error_description": "Unknown, used or expired code." }
{ "error": "unsupported_grant_type" }
```

---

### 3.11 FHIR R5: `/fhir/r5/...`

Every `/fhir/r4` endpoint also answers under `/fhir/r5`: read, search, `metadata`, `QuestionnaireResponse`, and the SMART
endpoints (`.well-known/smart-configuration`, `auth/authorize`, `auth/token`). Same data, same parameters, same
security; only the FHIR release of the body differs.

**How it works.** Inside the API everything stays R4: the mappers build R4 resources, exactly as before. For a call on
`/fhir/r5`, `Versioning/R5Converter` converts the finished R4 resource (or Bundle) to R5 just before it is sent.
The R4 answers did not change at all.

| Resource | R4 (as built) | R5 (as sent on `/fhir/r5`) |
|---|---|---|
| Coverage | `payor[]`, `subscriberId` text, `class.value` text | `insurer`, `subscriberId[]` Identifier, `class.value` Identifier, `kind` = `insurance` (required in R5) |
| Organization | `telecom[]`, `address[]` | `contact[]` with `telecom` and `address` |
| ServiceRequest | `code`, `locationCode[]` + `locationReference[]`, `reasonCode[]` | `code.concept`, `location[]`, `reason[]` (CodeableReference) |
| DeviceRequest | `codeCodeableConcept`, `performer`, `reasonCode[]` | `code.concept`, `performer.reference`, `reason[]` |
| MedicationRequest | `medicationCodeableConcept`, `reasonCode[]`, `dispenseRequest.performer` | `medication.concept`, `reason[]`, `dispenseRequest.dispenser` |
| Appointment | `serviceType[]` | `serviceType[].concept` |
| Encounter | `status` finished / arrived, `class` Coding, `period`, `participant.individual`, `reasonCode[]`, `hospitalization` | `status` completed / in-progress, `class[]` CodeableConcept, `actualPeriod`, `participant.actor`, `reason[].value[]`, `admission` |
| Procedure | `performedDateTime` | `occurrenceDateTime` |
| CapabilityStatement | `fhirVersion` 4.0.1, US Core guide | `fhirVersion` 5.0.0, no US Core guide |
| every resource | `meta.profile` = US Core / Da Vinci CRD profiles | no `meta.profile` (those profiles exist only for R4) |
| every URL | `.../fhir/r4/...` | `.../fhir/r5/...` (fullUrl, Binary links, SMART URLs, Location header) |

Patient, Practitioner, PractitionerRole, Location, Observation, Condition, DocumentReference, Binary, QuestionnaireResponse,
Bundle and OperationOutcome need no change for the elements this API writes. R5 responses carry
`Content-Type: application/fhir+json; fhirVersion=5.0`.

**Forms (QuestionnaireResponse).** The elements DTR uses are identical in R4 and R5. A form saved through `/fhir/r5` is
stored once, unchanged, and can be read through both releases. The Payer Gateway keeps reading forms through `/fhir/r4`.

**SMART.** A token issued on either release works on both. `POST /internal/smart-launches` takes an optional
`"fhirVersion": "r4" | "r5"` (default `r4`); with `r5` the returned `iss` is the R5 base URL, so the DTR app reads
`/fhir/r5`. Any other value returns 400.

**Settings.** `FhirServer:PublicBaseUrlR5` (default `http://localhost:5100/fhir/r5`), next to `FhirServer:PublicBaseUrl`.

**Limits.**
- There are no R5 versions of US Core or the Da Vinci profiles, so R5 answers claim no profile. They are valid R5
  resources (checked against the R5 core definitions), not profile-conformant ones.
- Only the elements the mappers write are converted. A new mapper field must be added to `R5Converter` too when R4
  and R5 differ for it.
- Errors (OperationOutcome) are the same in both releases and are sent with the R4 content type.


### 3.12 Payers' own SMART DTR apps

When a payer brings its own SMART DTR app (`PA_PAYER.DTR_MODE = SmartApp`), the flow is the standard SMART EHR launch,
with this API playing the EHR's part:

1. Order-sign: the payer's CRD card has a `smart` link. The gateway saves it and gives the EHR `/api/v1/dtr/launch/{linkId}`.
2. The user opens that link. The gateway calls `POST /internal/smart-launches` with the payer's FHIR release
   (`fhirVersion`, from `PA_PAYER.FHIR_VERSION`), then redirects the browser to the payer's app with `iss`
   (`/fhir/r4` or `/fhir/r5`) and `launch`.
3. The app reads `{iss}/.well-known/smart-configuration`, calls `auth/authorize`, then `auth/token`, and gets its context.
4. It reads the chart, calls its payer for the questionnaire, and saves the completed `QuestionnaireResponse` here.
   The form names its order in the DTR `qr-context` extension.
5. The EHR sees the form through the gateway's `GET /api/v1/dtr/saved-forms`; PAS attaches it automatically.

No app registration is needed here (section 2). When authentication is added outside this code, the payer's app
must be allowed through it.

## 4. Running and testing

1. Start `MockEhr.Api` (5090) and `FHIRAPI` (5100). In Visual Studio, use Configure Startup Projects → Multiple.
2. Open http://localhost:5100/swagger. Click **Authorize** and enter `local-internal-key` to call the protected endpoints.
3. Or run `FHIRAPI/FHIRAPI.http` top to bottom. It contains every example above, including the SMART launch-context flow.

**Storage locally:** `Storage:QuestionnaireResponseStore` is `EhrApi` and `Ehr:BaseUrl` points to the mock EHR, which implements the EHR data API (`MockEhr.Api/Controllers/PriorAuthDataController.cs`). Set it to `InMemory` to run without it.

---

## 5. Project structure

Each class lives in its own file, with the same name as the class.

| Folder | What's inside |
|---|---|
| `Program.cs` | Dependency-injection wiring and the request pipeline, nothing else |
| `Configuration/` | One settings class per `appsettings.json` section (`EhrSettings`, `FhirServerSettings`, `FhirMappingSettings`, `StorageSettings`). In `FhirMapping`, `MemberIdSystem` and `PayerIdSystem` are optional and empty by default: the member id and the payer id of an insurance are sent without a "system" unless a payer gives one |
| `Constants/` | URIs and fixed values: code systems, profiles, DTR extension URLs, issue codes, headers, default scopes |
| `Controllers/` | One controller per area: `MetadataController`, `FhirResourceController` (read + search), `QuestionnaireResponseController`, `SmartAuthController`, `InternalController` |
| `Middleware/` | `FhirExceptionHandler` (every error → OperationOutcome, so controllers have no try/catch), correlation id middleware and handler |
| `Services/` | `EhrClient` (HTTP to the EHR), `FhirReadService` (read one), `FhirSearchService` (search + `_include`), `ReferenceFinder` (references and patient of a resource), `QuestionnaireResponseService`, the SMART launch store, `CoveragePatientIndex` |
| `Mapping/` | EHR → FHIR converters split by area: `AdministrativeMapper`, `OrderMapper`, `ClinicalMapper`; `QuestionnaireResponseMapper` (FHIR → EHR); `CodeSystemMapper` (CPT → URI and similar) |
| `Repositories/Interfaces/` | `IQuestionnaireResponseRepository` and `QuestionnaireResponseRecord`: what the services use |
| `Repositories/Database/` | `EhrApiQuestionnaireResponseRepository`: saves forms to the EHR database through the EHR data API |
| `Repositories/InMemory/` | `InMemoryQuestionnaireResponseRepository`: keeps forms in memory for local testing |
| `Models/Fhir/` | Own FHIR classes: `DataTypes/`, `Base/`, `Resources/` (no FHIR SDK) |
| `Models/Ehr/` | EHR REST DTOs |
| `Models/Requests/` | Request and response bodies of the non-FHIR endpoints (launches, SMART) plus `FhirSearchParameters` |
| `Models/Security/` | `SmartLaunch`, `AuthorizationCode` |
| `Exceptions/` | `FhirException` (4xx), `EhrException` (502) |
| `Helpers/` | `FhirJson` (serializer settings), `FhirResults`, `FhirDate`, `RandomIds` |

## 6. What changed from the previous version

| Area | Before | Now |
|---|---|---|
| Files | Several classes per file (`Security.cs` had 9, `FhirDataService.cs` had 4, `Resources.cs` had 20+) | One class per file, 130+ files in clear folders |
| Search | Parameters read by hand, so Swagger couldn't send them (the "not working" issue) | Declared parameters, Swagger boxes, clear 400 messages |
| `_include` | Resources serialized to JSON and walked as a JSON tree | Typed `ReferenceFinder` switch, readable per resource type |
| Error handling | try/catch in every action | One `FhirExceptionHandler` |
| QuestionnaireResponse | In memory; body re-serialized, so unmodelled fields could be lost | EHR database through the EHR data API (or memory for local), exact JSON stored; EHR sync status tracked |
| QuestionnaireResponse `POST` in Swagger | No body box | `[FromBody]` JSON, accepts `application/fhir+json` |
| PUT with an existing id | Always 200 | 201 on create, 200 on update, id mismatch → 400 |
| Patient-limited tokens | Binary readable by any token | Binary checked against its DocumentReference's patient |
| Mapper | One 450-line class | Four mappers by area + code-system mapper |
| FHIR models | Included unused payer-side models (Task, Subscription, Library, ValueSet, Parameters...) | Only what this API produces or reads |
| Audit | Separate in-memory audit log | Structured `AUDIT` log lines through ILogger (send to your log store) |

## 7. Before production

- Add authentication outside this code (section 2), and restrict CORS to the DTR apps' origins.
- SMART launches are held in memory. That's fine for one instance; with several instances, move them to a shared store such as Redis, or to an EHR data API table (the class sits behind an interface).
- Expose only `/fhir/r4/*` and `/fhir/r5/*` publicly. Keep `/internal/*` private.
- Add a small job that re-sends forms with `EHR_SYNC_STATUS = 'FAILED'` to the EHR.
