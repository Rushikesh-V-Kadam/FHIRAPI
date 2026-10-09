using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Helpers;
using MockEhr.Api.Models;
using MockEhr.Api.Requests;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>
/// Patients and everything filed under a patient: insurance, encounters, observations, conditions, procedures,
/// documents and DTR questionnaire responses. GET endpoints are the EHR contract read by the FHIR API;
/// POST / PUT endpoints are used by the React EHR app (and questionnaire-responses by the FHIR API).
/// </summary>
[ApiController]
[Route("api/patients")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class PatientsController : ControllerBase
{
    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public PatientsController(EhrStore store)
    {
        _store = store;
    }

    // ================================================================== patients

    /// <summary>All patients (optional search).</summary>
    /// <param name="search">Text to look for in the name, MRN or patient id. Empty = all patients.</param>
    /// <response code="200">The patients.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet]
    [ProducesResponseType(typeof(EhrList<EhrPatientDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrPatientDto>> List([FromQuery] string? search)
    {
        List<EhrPatientDto> result = new List<EhrPatientDto>();
        lock (_store.Sync)
        {
            foreach (EhrPatientDto patient in _store.Data.Patients)
            {
                if (string.IsNullOrWhiteSpace(search) || Matches(patient, search))
                {
                    result.Add(patient);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>One patient.</summary>
    /// <remarks>
    /// The FHIR API turns the answer into a FHIR Patient (GET /fhir/r4/Patient/{id}).
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <response code="200">The patient.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead, SwaggerGroups.MockUi)]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EhrPatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrPatientDto> Get(string id)
    {
        lock (_store.Sync)
        {
            EhrPatientDto? patient = FindPatient(id);
            if (patient == null)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            return Ok(patient);
        }
    }

    /// <summary>Adds a patient.</summary>
    /// <remarks>
    /// firstName, lastName, dateOfBirth and gender must be filled. An empty patientId gets a new id.
    /// </remarks>
    /// <param name="patient">Required. The patient to add.</param>
    /// <response code="201">The patient as stored (with its id).</response>
    /// <response code="400">A required field is missing.</response>
    /// <response code="409">A patient with this id already exists.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrPatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public ActionResult<EhrPatientDto> Create([FromBody, Required] EhrPatientDto patient)
    {
        string? problem = CheckPatient(patient);
        if (problem != null)
        {
            return BadRequest(new ErrorResponse(problem));
        }

        lock (_store.Sync)
        {
            if (string.IsNullOrWhiteSpace(patient.PatientId))
            {
                patient.PatientId = EhrStore.NewId("pat");
            }
            if (FindPatient(patient.PatientId) != null)
            {
                return Conflict(new ErrorResponse("Patient " + patient.PatientId + " already exists."));
            }
            if (string.IsNullOrWhiteSpace(patient.Mrn))
            {
                patient.Mrn = "MRN-" + patient.PatientId.ToUpperInvariant();
            }
            if (patient.Active == null)
            {
                patient.Active = true;
            }
            _store.Data.Patients.Add(patient);
            _store.Save();
        }
        return Created("/api/patients/" + patient.PatientId, patient);
    }

    /// <summary>Changes a patient.</summary>
    /// <remarks>
    /// firstName, lastName, dateOfBirth and gender must be filled. The id in the URL wins over patientId in the body.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="patient">Required. The whole patient (replaces the stored one).</param>
    /// <response code="200">The patient as stored.</response>
    /// <response code="400">A required field is missing.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrPatientDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrPatientDto> Update(string id, [FromBody, Required] EhrPatientDto patient)
    {
        string? problem = CheckPatient(patient);
        if (problem != null)
        {
            return BadRequest(new ErrorResponse(problem));
        }

        lock (_store.Sync)
        {
            int index = -1;
            for (int i = 0; i < _store.Data.Patients.Count; i++)
            {
                if (_store.Data.Patients[i].PatientId == id)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            patient.PatientId = id;
            _store.Data.Patients[index] = patient;
            _store.Save();
        }
        return Ok(patient);
    }

    // ================================================================== insurance

    /// <summary>Insurance (coverage) of a patient.</summary>
    /// <remarks>
    /// The FHIR API calls it with status=active and turns each row into a FHIR Coverage (GET /fhir/r4/Coverage?patient={id}).
    /// A patient without insurance gives an empty list, not 404.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="status">Only insurance with this status, e.g. active. Empty = all.</param>
    /// <response code="200">The insurance rows.</response>
    [Tags(SwaggerGroups.FhirApiRead, SwaggerGroups.MockUi)]
    [HttpGet("{id}/insurances")]
    [ProducesResponseType(typeof(EhrList<EhrInsuranceDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrInsuranceDto>> Insurances(string id, [FromQuery] string? status)
    {
        List<EhrInsuranceDto> result = new List<EhrInsuranceDto>();
        lock (_store.Sync)
        {
            foreach (EhrInsuranceDto insurance in _store.Data.Insurances)
            {
                if (insurance.PatientId == id && QueryFilter.SameText(status, insurance.Status))
                {
                    result.Add(insurance);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>Adds insurance to a patient.</summary>
    /// <remarks>
    /// memberId and payerId must be filled. An empty coverageId gets a new id; an empty status becomes active.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="insurance">Required. The insurance to add.</param>
    /// <response code="201">The insurance as stored (with its id).</response>
    /// <response code="400">memberId or payerId is missing.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost("{id}/insurances")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrInsuranceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrInsuranceDto> AddInsurance(string id, [FromBody, Required] EhrInsuranceDto insurance)
    {
        if (string.IsNullOrWhiteSpace(insurance.MemberId) || string.IsNullOrWhiteSpace(insurance.PayerId))
        {
            return BadRequest(new ErrorResponse("memberId and payerId are required."));
        }

        lock (_store.Sync)
        {
            if (FindPatient(id) == null)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            insurance.PatientId = id;
            if (string.IsNullOrWhiteSpace(insurance.CoverageId))
            {
                insurance.CoverageId = EhrStore.NewId("cov");
            }
            if (string.IsNullOrWhiteSpace(insurance.Status))
            {
                insurance.Status = "active";
            }
            _store.Data.Insurances.Add(insurance);
            _store.Save();
        }
        return Created("/api/patients/" + id + "/insurances", insurance);
    }

    // ================================================================== clinical data

    /// <summary>Encounters (visits) of a patient.</summary>
    /// <remarks>
    /// The FHIR API turns each row into a FHIR Encounter (GET /fhir/r4/Encounter?patient={id}).
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="from">Only encounters that started on or after this date (yyyy-MM-dd or a full UTC date-time). Empty = all.</param>
    /// <response code="200">The encounters.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}/encounters")]
    [ProducesResponseType(typeof(EhrList<EhrEncounterDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrEncounterDto>> Encounters(string id, [FromQuery] string? from)
    {
        List<EhrEncounterDto> result = new List<EhrEncounterDto>();
        lock (_store.Sync)
        {
            foreach (EhrEncounterDto encounter in _store.Data.Encounters)
            {
                if (encounter.PatientId == id && QueryFilter.OnOrAfter(encounter.StartDateTime, from))
                {
                    result.Add(encounter);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>Observations (lab results, vital signs) of a patient.</summary>
    /// <remarks>
    /// The FHIR API turns each row into a FHIR Observation (GET /fhir/r4/Observation?patient={id}).
    /// Payers and DTR forms read lab values this way, so the codes filter is used a lot.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="codes">Only these codes, comma separated. A code can have its code system in front: LOINC|33914-3,LOINC|2160-0. Empty = all.</param>
    /// <param name="category">Only this category: laboratory or vital-signs. Empty = all.</param>
    /// <param name="from">Only results measured on or after this date (yyyy-MM-dd or a full UTC date-time). Empty = all.</param>
    /// <response code="200">The observations.</response>
    [Tags(SwaggerGroups.FhirApiRead, SwaggerGroups.MockUi)]
    [HttpGet("{id}/observations")]
    [ProducesResponseType(typeof(EhrList<EhrObservationDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrObservationDto>> Observations(string id, [FromQuery] string? codes, [FromQuery] string? category, [FromQuery] string? from)
    {
        List<EhrObservationDto> result = new List<EhrObservationDto>();
        lock (_store.Sync)
        {
            foreach (EhrObservationDto observation in _store.Data.Observations)
            {
                bool matches = observation.PatientId == id
                               && QueryFilter.CodeMatches(codes, observation.Code, observation.CodeSystem)
                               && QueryFilter.SameText(category, observation.Category)
                               && QueryFilter.OnOrAfter(observation.EffectiveDateTime, from);
                if (matches)
                {
                    result.Add(observation);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>Adds an observation (lab result or vital sign).</summary>
    /// <remarks>
    /// code must be filled. An empty observationId gets a new id; an empty effectiveDateTime becomes now.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="observation">Required. The observation to add.</param>
    /// <response code="201">The observation as stored (with its id).</response>
    /// <response code="400">code is missing.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost("{id}/observations")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrObservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrObservationDto> AddObservation(string id, [FromBody, Required] EhrObservationDto observation)
    {
        if (string.IsNullOrWhiteSpace(observation.Code))
        {
            return BadRequest(new ErrorResponse("code is required."));
        }
        lock (_store.Sync)
        {
            if (FindPatient(id) == null)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            observation.PatientId = id;
            if (string.IsNullOrWhiteSpace(observation.ObservationId))
            {
                observation.ObservationId = EhrStore.NewId("obs");
            }
            if (string.IsNullOrWhiteSpace(observation.EffectiveDateTime))
            {
                observation.EffectiveDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            }
            _store.Data.Observations.Add(observation);
            _store.Save();
        }
        return Created("/api/patients/" + id + "/observations", observation);
    }

    /// <summary>Conditions (diagnoses) of a patient.</summary>
    /// <remarks>
    /// The FHIR API turns each row into a FHIR Condition (GET /fhir/r4/Condition?patient={id}).
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="codes">Only these codes, comma separated. A code can have its code system in front: ICD10CM|N18.6. Empty = all.</param>
    /// <param name="status">Only conditions with this clinical status, e.g. active. Empty = all.</param>
    /// <response code="200">The conditions.</response>
    [Tags(SwaggerGroups.FhirApiRead, SwaggerGroups.MockUi)]
    [HttpGet("{id}/conditions")]
    [ProducesResponseType(typeof(EhrList<EhrConditionDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrConditionDto>> Conditions(string id, [FromQuery] string? codes, [FromQuery] string? status)
    {
        List<EhrConditionDto> result = new List<EhrConditionDto>();
        lock (_store.Sync)
        {
            foreach (EhrConditionDto condition in _store.Data.Conditions)
            {
                bool matches = condition.PatientId == id
                               && QueryFilter.CodeMatches(codes, condition.Code, condition.CodeSystem)
                               && QueryFilter.SameText(status, condition.ClinicalStatus);
                if (matches)
                {
                    result.Add(condition);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>Adds a condition (diagnosis on the problem list).</summary>
    /// <remarks>
    /// code must be filled. An empty conditionId gets a new id; an empty clinicalStatus becomes active.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="condition">Required. The condition to add.</param>
    /// <response code="201">The condition as stored (with its id).</response>
    /// <response code="400">code is missing.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost("{id}/conditions")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrConditionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrConditionDto> AddCondition(string id, [FromBody, Required] EhrConditionDto condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Code))
        {
            return BadRequest(new ErrorResponse("code is required."));
        }
        lock (_store.Sync)
        {
            if (FindPatient(id) == null)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            condition.PatientId = id;
            if (string.IsNullOrWhiteSpace(condition.ConditionId))
            {
                condition.ConditionId = EhrStore.NewId("cond");
            }
            if (string.IsNullOrWhiteSpace(condition.ClinicalStatus))
            {
                condition.ClinicalStatus = "active";
            }
            if (string.IsNullOrWhiteSpace(condition.Category))
            {
                condition.Category = "problem-list-item";
            }
            _store.Data.Conditions.Add(condition);
            _store.Save();
        }
        return Created("/api/patients/" + id + "/conditions", condition);
    }

    /// <summary>Procedures done for a patient.</summary>
    /// <remarks>
    /// The FHIR API turns each row into a FHIR Procedure (GET /fhir/r4/Procedure?patient={id}).
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="codes">Only these codes, comma separated. A code can have its code system in front: CPT|36821. Empty = all.</param>
    /// <param name="from">Only procedures done on or after this date (yyyy-MM-dd or a full UTC date-time). Empty = all.</param>
    /// <response code="200">The procedures.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("{id}/procedures")]
    [ProducesResponseType(typeof(EhrList<EhrProcedureDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrProcedureDto>> Procedures(string id, [FromQuery] string? codes, [FromQuery] string? from)
    {
        List<EhrProcedureDto> result = new List<EhrProcedureDto>();
        lock (_store.Sync)
        {
            foreach (EhrProcedureDto procedure in _store.Data.Procedures)
            {
                bool matches = procedure.PatientId == id
                               && QueryFilter.CodeMatches(codes, procedure.Code, procedure.CodeSystem)
                               && QueryFilter.OnOrAfter(procedure.PerformedDateTime, from);
                if (matches)
                {
                    result.Add(procedure);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    // ================================================================== documents

    /// <summary>Clinical documents of a patient (the descriptions, not the files).</summary>
    /// <remarks>
    /// The FHIR API turns each row into a FHIR DocumentReference (GET /fhir/r4/DocumentReference?patient={id}).
    /// The gateway looks for the documents a payer asked for by LOINC type this way.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="typeCode">Only documents of these LOINC types, comma separated, with or without the code system in front: LOINC|11506-3. Empty = all.</param>
    /// <response code="200">The documents.</response>
    [Tags(SwaggerGroups.FhirApiRead, SwaggerGroups.MockUi)]
    [HttpGet("{id}/documents")]
    [ProducesResponseType(typeof(EhrList<EhrDocumentDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<EhrDocumentDto>> Documents(string id, [FromQuery] string? typeCode)
    {
        List<EhrDocumentDto> result = new List<EhrDocumentDto>();
        lock (_store.Sync)
        {
            foreach (EhrDocumentDto document in _store.Data.Documents)
            {
                if (document.PatientId == id && QueryFilter.CodeMatches(typeCode, document.TypeCode, document.TypeCodeSystem))
                {
                    result.Add(document);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>Adds a clinical document (typed text or an uploaded file as Base64).</summary>
    /// <remarks>
    /// typeCode (LOINC) must be filled, and contentText or contentBase64.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="request">Required. The document and its content.</param>
    /// <response code="201">The document as stored (with its id and size).</response>
    /// <response code="400">typeCode or the content is missing, or contentBase64 is not valid Base64.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpPost("{id}/documents")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(EhrDocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<EhrDocumentDto> AddDocument(string id, [FromBody, Required] NewDocumentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TypeCode))
        {
            return BadRequest(new ErrorResponse("typeCode (LOINC) is required."));
        }

        string contentBase64;
        if (!string.IsNullOrWhiteSpace(request.ContentBase64))
        {
            contentBase64 = request.ContentBase64;
            try
            {
                Convert.FromBase64String(contentBase64);
            }
            catch (FormatException)
            {
                return BadRequest(new ErrorResponse("contentBase64 is not valid Base64."));
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.ContentText))
        {
            contentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.ContentText));
        }
        else
        {
            return BadRequest(new ErrorResponse("Send contentText or contentBase64."));
        }

        EhrDocumentDto document = new EhrDocumentDto();
        lock (_store.Sync)
        {
            if (FindPatient(id) == null)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            document.DocumentId = string.IsNullOrWhiteSpace(request.DocumentId) ? EhrStore.NewId("doc") : request.DocumentId;
            document.PatientId = id;
            document.TypeCode = request.TypeCode;
            document.TypeDisplay = request.TypeDisplay;
            document.Category = "clinical-note";
            document.Title = request.Title ?? request.TypeDisplay;
            document.CreatedDateTime = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
            document.AuthorPractitionerId = request.AuthorPractitionerId;
            document.OrganizationId = request.OrganizationId;
            document.EncounterId = request.EncounterId;
            document.ContentType = request.ContentType;
            SeedData.AddDocument(_store.Data, document, contentBase64);
            _store.Save();
        }
        return Created("/api/documents/" + document.DocumentId, document);
    }

    // ================================================================== DTR forms (written by the FHIR API)

    /// <summary>DTR forms stored in the chart of a patient (readable copies).</summary>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <response code="200">The forms.</response>
    [Tags(SwaggerGroups.MockUi)]
    [HttpGet("{id}/questionnaire-responses")]
    [ProducesResponseType(typeof(EhrList<QuestionnaireResponseDto>), StatusCodes.Status200OK)]
    public ActionResult<EhrList<QuestionnaireResponseDto>> QuestionnaireResponses(string id)
    {
        List<QuestionnaireResponseDto> result = new List<QuestionnaireResponseDto>();
        lock (_store.Sync)
        {
            foreach (QuestionnaireResponseDto response in _store.Data.QuestionnaireResponses)
            {
                if (response.PatientId == id)
                {
                    result.Add(response);
                }
            }
        }
        return Ok(ListResult.Of(result));
    }

    /// <summary>Stores the readable copy of a new DTR form in the patient's chart (called by the FHIR API).</summary>
    /// <remarks>
    /// The FHIR API calls this after it saved the form in table PA_QUESTIONNAIRE_RESPONSE, so the clinician can see the answers in the chart.
    /// The EHR creates responseId and returns the stored form; the FHIR API keeps that id (column EHR_RESPONSE_ID) and uses it for later updates.
    /// If the call fails the form is still saved (EHR_SYNC_STATUS = FAILED) and can be copied again later.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="response">Required. The form: answers in plain JSON, plus the complete FHIR JSON in fhirJson.</param>
    /// <response code="201">The form as stored, with the responseId the EHR gave it.</response>
    /// <response code="404">No patient with this id.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpPost("{id}/questionnaire-responses")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(QuestionnaireResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public ActionResult<QuestionnaireResponseDto> AddQuestionnaireResponse(string id, [FromBody, Required] QuestionnaireResponseDto response)
    {
        lock (_store.Sync)
        {
            if (FindPatient(id) == null)
            {
                return NotFound(new ErrorResponse("Patient " + id + " not found."));
            }
            _store.Data.QrCounter = _store.Data.QrCounter + 1;
            response.ResponseId = "qr-" + _store.Data.QrCounter;
            response.PatientId = id;
            _store.Data.QuestionnaireResponses.Add(response);
            _store.LogWriteBack("questionnaire-response", response.ResponseId, response);
            _store.Save();
        }
        return Created("/api/patients/" + id + "/questionnaire-responses/" + response.ResponseId, response);
    }

    /// <summary>Replaces the readable copy of a DTR form in the patient's chart (called by the FHIR API).</summary>
    /// <remarks>
    /// Called when a form that was saved before is saved again (more answers, or completed). The ids in the URL win over the ids in the body.
    /// An unknown responseId is stored as a new form.
    /// </remarks>
    /// <param name="id" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <param name="responseId" example="qr-1">Id the EHR gave the form when it was first stored, e.g. qr-1.</param>
    /// <param name="response">Required. The whole form (replaces the stored one).</param>
    /// <response code="200">The form as stored.</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpPut("{id}/questionnaire-responses/{responseId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(QuestionnaireResponseDto), StatusCodes.Status200OK)]
    public ActionResult<QuestionnaireResponseDto> UpdateQuestionnaireResponse(string id, string responseId, [FromBody, Required] QuestionnaireResponseDto response)
    {
        lock (_store.Sync)
        {
            response.ResponseId = responseId;
            response.PatientId = id;
            int index = -1;
            for (int i = 0; i < _store.Data.QuestionnaireResponses.Count; i++)
            {
                if (_store.Data.QuestionnaireResponses[i].ResponseId == responseId)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                _store.Data.QuestionnaireResponses.Add(response);
            }
            else
            {
                _store.Data.QuestionnaireResponses[index] = response;
            }
            _store.LogWriteBack("questionnaire-response", responseId, response);
            _store.Save();
        }
        return Ok(response);
    }

    // ================================================================== helpers

    /// <summary>The patient with this id, or null (call inside the lock).</summary>
    private EhrPatientDto? FindPatient(string id)
    {
        foreach (EhrPatientDto patient in _store.Data.Patients)
        {
            if (patient.PatientId == id)
            {
                return patient;
            }
        }
        return null;
    }

    /// <summary>Required patient fields; null when fine.</summary>
    private static string? CheckPatient(EhrPatientDto patient)
    {
        if (string.IsNullOrWhiteSpace(patient.FirstName) || string.IsNullOrWhiteSpace(patient.LastName))
        {
            return "firstName and lastName are required.";
        }
        if (string.IsNullOrWhiteSpace(patient.DateOfBirth))
        {
            return "dateOfBirth (yyyy-MM-dd) is required.";
        }
        if (string.IsNullOrWhiteSpace(patient.Gender))
        {
            return "gender is required (male, female, other, unknown).";
        }
        return null;
    }

    /// <summary>True when the search text is in the name or MRN.</summary>
    private static bool Matches(EhrPatientDto patient, string search)
    {
        string text = (patient.FirstName + " " + patient.LastName + " " + patient.Mrn + " " + patient.PatientId).ToLowerInvariant();
        return text.Contains(search.Trim().ToLowerInvariant());
    }
}
