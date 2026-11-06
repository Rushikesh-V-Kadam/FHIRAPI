using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MockEhr.Api.Constants;
using MockEhr.Api.Filters;
using MockEhr.Api.Models;
using MockEhr.Api.Models.Client;
using MockEhr.Api.Storage;

namespace MockEhr.Api.Controllers;

/// <summary>
/// The client EHR's patient list APIs, served from the mock data in the client's format, so the FHIR API can be tested
/// against the same paths and JSON the real EHR has:
///   GET Patients/getproclst?patientID=... -> { "ProcedureDetailList": [...], "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
///   GET Patients/getconlst?patientID=...  -> { "ConditionDetailList": [...], ... }
///   GET Patients/getobslst?patientID=...  -> { "ObservationDetailList": [...], ... }
/// Like the client's APIs: no filters, no ids, no code systems, dates in US Central time without a zone.
/// An unknown patient gives an empty list.
/// </summary>
[ApiController]
[Route("Patients")]
[Produces("application/json")]
[ServiceFilter(typeof(ApiKeyFilter))]
public class ClientPatientListsController : ControllerBase
{
    /// <summary>The client's EHR writes date-times in US Central time.</summary>
    private const string CentralTimeZoneId = "America/Chicago";

    /// <summary>Windows name of the same time zone.</summary>
    private const string CentralTimeZoneWindowsId = "Central Standard Time";

    private readonly EhrStore _store;

    /// <summary>Created by dependency injection.</summary>
    public ClientPatientListsController(EhrStore store)
    {
        _store = store;
    }

    /// <summary>Procedures of a patient, in the client EHR's format.</summary>
    /// <remarks>The FHIR API turns each row into a FHIR Procedure (GET /fhir/r4/Procedure?patient={id}).</remarks>
    /// <param name="patientId" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <response code="200">The procedures (an empty list for an unknown patient).</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("getproclst")]
    [ProducesResponseType(typeof(ProcedureListResponse), StatusCodes.Status200OK)]
    public ActionResult<ProcedureListResponse> Procedures([FromQuery(Name = "patientID")] string? patientId)
    {
        ProcedureListResponse answer = new ProcedureListResponse();
        lock (_store.Sync)
        {
            foreach (EhrProcedureDto procedure in _store.Data.Procedures)
            {
                if (procedure.PatientId != patientId)
                {
                    continue;
                }
                ClientProcedureRow row = new ClientProcedureRow();
                row.ProcedureId = null;   // the client sends ProcedureID as null
                row.PatientId = procedure.PatientId;
                row.ProcedureCode = procedure.Code;
                row.ProcedureName = procedure.Display;
                row.ProcedureStatus = procedure.Status;
                row.PerformedDateTime = ToCentral(procedure.PerformedDateTime);
                row.PerformerPractitionerNpi = PractitionerNpi(procedure.PerformerPractitionerId);
                answer.ProcedureDetailList.Add(row);
            }
        }
        return Ok(answer);
    }

    /// <summary>Diagnoses of a patient, in the client EHR's format.</summary>
    /// <remarks>The FHIR API turns each row into a FHIR Condition (GET /fhir/r4/Condition?patient={id}).</remarks>
    /// <param name="patientId" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <response code="200">The diagnoses (an empty list for an unknown patient).</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("getconlst")]
    [ProducesResponseType(typeof(ConditionListResponse), StatusCodes.Status200OK)]
    public ActionResult<ConditionListResponse> Conditions([FromQuery(Name = "patientID")] string? patientId)
    {
        ConditionListResponse answer = new ConditionListResponse();
        lock (_store.Sync)
        {
            foreach (EhrConditionDto condition in _store.Data.Conditions)
            {
                if (condition.PatientId != patientId)
                {
                    continue;
                }
                ClientConditionRow row = new ClientConditionRow();
                row.PatientId = condition.PatientId;
                row.DiagnosisCode = condition.Code;
                row.DiagnosisDescription = condition.Display;
                row.Category = condition.Category;
                row.DiagnosisStatus = condition.ClinicalStatus;
                answer.ConditionDetailList.Add(row);
            }
        }
        return Ok(answer);
    }

    /// <summary>Lab results of a patient, in the client EHR's format (vital signs are not in the client's list).</summary>
    /// <remarks>The FHIR API turns each row into a FHIR Observation (GET /fhir/r4/Observation?patient={id}).</remarks>
    /// <param name="patientId" example="pat-1">EHR patient id, e.g. pat-1.</param>
    /// <response code="200">The lab results (an empty list for an unknown patient).</response>
    [Tags(SwaggerGroups.FhirApiRead)]
    [HttpGet("getobslst")]
    [ProducesResponseType(typeof(ObservationListResponse), StatusCodes.Status200OK)]
    public ActionResult<ObservationListResponse> Observations([FromQuery(Name = "patientID")] string? patientId)
    {
        ObservationListResponse answer = new ObservationListResponse();
        lock (_store.Sync)
        {
            foreach (EhrObservationDto observation in _store.Data.Observations)
            {
                if (observation.PatientId != patientId)
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(observation.Category)
                    && !string.Equals(observation.Category, "laboratory", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                ClientObservationRow row = new ClientObservationRow();
                row.PatientId = observation.PatientId;
                row.LabStatus = observation.Status;
                row.LabTestCode = observation.Code;
                row.EffectiveDate = ToCentral(observation.EffectiveDateTime);
                row.LabTestName = observation.Display;
                row.LabResultValue = ResultText(observation);
                row.LabResultDescription = observation.Display;
                row.LabUnit = observation.Unit;
                answer.ObservationDetailList.Add(row);
            }
        }
        return Ok(answer);
    }

    /// <summary>The result as text, like the client sends it ("64.2").</summary>
    private static string? ResultText(EhrObservationDto observation)
    {
        if (observation.ValueNumber != null)
        {
            return observation.ValueNumber.Value.ToString(CultureInfo.InvariantCulture);
        }
        if (!string.IsNullOrEmpty(observation.ValueText))
        {
            return observation.ValueText;
        }
        if (observation.ValueCode != null)
        {
            return observation.ValueCode.Display ?? observation.ValueCode.Code;
        }
        return null;
    }

    /// <summary>NPI of a mock practitioner, or null. Call inside lock (_store.Sync).</summary>
    private string? PractitionerNpi(string? practitionerId)
    {
        if (string.IsNullOrEmpty(practitionerId))
        {
            return null;
        }
        foreach (EhrPractitionerDto practitioner in _store.Data.Practitioners)
        {
            if (practitioner.PractitionerId == practitionerId)
            {
                return practitioner.Npi;
            }
        }
        return null;
    }

    /// <summary>
    /// A stored date-time (UTC, e.g. 2026-09-22T07:00:00Z) as US Central time without a zone (2026-09-22T02:00:00),
    /// the way the client's EHR writes it. A date only, or a value that cannot be read, is returned unchanged.
    /// </summary>
    private static string? ToCentral(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length == 10)
        {
            return value;
        }
        DateTimeOffset instant;
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out instant))
        {
            return value;
        }
        DateTimeOffset central = TimeZoneInfo.ConvertTime(instant, FindCentral());
        return central.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>US Central time zone (IANA name first, then the Windows name).</summary>
    private static TimeZoneInfo FindCentral()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(CentralTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(CentralTimeZoneWindowsId);
        }
    }
}
