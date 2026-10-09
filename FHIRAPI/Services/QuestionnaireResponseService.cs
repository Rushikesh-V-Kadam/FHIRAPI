using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Mapping;
using FHIRAPI.Models.Fhir;
using FHIRAPI.Repositories.Interfaces;

namespace FHIRAPI.Services;

/// <summary>
/// Saves and reads DTR forms (FHIR QuestionnaireResponse).
///
/// Saving a form does three things:
///   1. checks the body (must be a QuestionnaireResponse with subject = Patient/{id});
///   2. writes a flattened copy to the EHR (so clinicians see the form in the chart);
///   3. stores the complete FHIR JSON in table PA_QUESTIONNAIRE_RESPONSE (the source of truth for FHIR reads).
/// If the EHR is down the form is still stored, with EhrSyncStatus = "FAILED", so no data is lost.
/// </summary>
public class QuestionnaireResponseService
{
    private readonly IQuestionnaireResponseRepository _repository;
    private readonly IEhrClient _ehr;
    private readonly QuestionnaireResponseMapper _mapper;
    private readonly FhirServerSettings _server;
    private readonly ILogger<QuestionnaireResponseService> _logger;

    /// <summary>Created by dependency injection.</summary>
    public QuestionnaireResponseService(IQuestionnaireResponseRepository repository, IEhrClient ehr,
        QuestionnaireResponseMapper mapper, IOptions<FhirServerSettings> server, ILogger<QuestionnaireResponseService> logger)
    {
        _repository = repository;
        _ehr = ehr;
        _mapper = mapper;
        _server = server.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates (POST, id = null) or updates (PUT, id from the URL) a form.
    /// </summary>
    /// <param name="idFromUrl">Id from PUT /QuestionnaireResponse/{id}; null for POST (a new id is generated).</param>
    /// <param name="body">The JSON body sent by the caller.</param>
    /// <returns>The stored record and whether it was newly created.</returns>
    public async Task<(QuestionnaireResponseRecord Record, bool Created)> SaveAsync(
        string? idFromUrl, JsonElement body, CancellationToken ct)
    {
        // 1. Validate the body ------------------------------------------------------------------
        var json = JsonNode.Parse(body.GetRawText()) as JsonObject
                   ?? throw new FhirException(400, IssueTypes.Invalid, "The body must be a JSON object.");

        if (json["resourceType"]?.GetValue<string>() != "QuestionnaireResponse")
            throw new FhirException(400, IssueTypes.Invalid, "resourceType must be \"QuestionnaireResponse\".");

        var bodyId = json["id"]?.GetValue<string>();
        if (idFromUrl != null && bodyId != null && bodyId != idFromUrl)
            throw new FhirException(400, IssueTypes.Invalid, $"The id in the body ({bodyId}) must match the id in the URL ({idFromUrl}).");

        // PUT keeps the URL id; POST always gets a new server id (FHIR create rule)
        var id = idFromUrl ?? Guid.NewGuid().ToString("N");
        json["id"] = id;
        json["meta"] ??= new JsonObject();
        json["meta"]!["lastUpdated"] = FhirDate.Now();

        var form = FhirJson.Deserialize<QuestionnaireResponse>(json.ToJsonString())
                   ?? throw new FhirException(400, IssueTypes.Invalid, "The body is not a valid QuestionnaireResponse.");

        var patientId = form.Subject?.GetResourceType() == "Patient" ? form.Subject.GetId() : null;
        if (string.IsNullOrEmpty(patientId))
            throw new FhirException(400, IssueTypes.Required, "QuestionnaireResponse.subject must reference a Patient, e.g. \"Patient/pat-1\".");

        // Which order / coverage the form belongs to (DTR qr-context extension)
        var orderId = FindContextId(form, isCoverage: false);
        var coverageId = FindContextId(form, isCoverage: true);

        var existing = await _repository.GetAsync(id, ct);
        var fullJson = json.ToJsonString();

        var record = new QuestionnaireResponseRecord
        {
            Id = id,
            PatientId = patientId,
            OrderId = orderId ?? existing?.OrderId,
            CoverageId = coverageId ?? existing?.CoverageId,
            QuestionnaireUrl = form.Questionnaire?.Split('|')[0],
            Status = form.Status,
            ResourceJson = fullJson,
            EhrResponseId = existing?.EhrResponseId
        };

        // 2. Copy to the EHR (failure is logged, not fatal) --------------------------------------
        try
        {
            var ehrForm = _mapper.ToEhr(form, fullJson, patientId, record.OrderId, record.CoverageId, record.EhrResponseId);
            var saved = await _ehr.SaveQuestionnaireResponseAsync(ehrForm, ct);
            record.EhrResponseId = string.IsNullOrEmpty(saved.ResponseId) ? record.EhrResponseId : saved.ResponseId;
            record.EhrSyncStatus = "SENT";
        }
        catch (EhrException ex)
        {
            record.EhrSyncStatus = "FAILED";
            _logger.LogWarning(ex, "QuestionnaireResponse {Id} stored, but the EHR write-back failed", id);
        }

        // 3. Store (EHR database through the EHR data API, or memory) -------------------------------
        await _repository.SaveAsync(record, ct);
        _logger.LogInformation("AUDIT write QuestionnaireResponse/{Id} patient={PatientId} ehr={Sync}", id, patientId, record.EhrSyncStatus);

        return (record, existing == null);
    }

    /// <summary>Returns a stored form, or null.</summary>
    public Task<QuestionnaireResponseRecord?> GetAsync(string id, CancellationToken ct) => _repository.GetAsync(id, ct);

    /// <summary>All forms of a patient as a FHIR searchset Bundle.</summary>
    public async Task<Bundle> SearchByPatientAsync(string patientId, CancellationToken ct)
    {
        var records = await _repository.GetByPatientAsync(patientId, ct);

        var bundle = new Bundle { Id = Guid.NewGuid().ToString("N"), Timestamp = FhirDate.Now(), Total = records.Count };
        bundle.Link.Add(new BundleLink { Relation = "self", Url = $"{_server.BaseUrl}/QuestionnaireResponse?patient={patientId}" });

        foreach (var record in records.Take(_server.MaxPageSize))
        {
            bundle.Entry.Add(new BundleEntry
            {
                FullUrl = $"{_server.BaseUrl}/QuestionnaireResponse/{record.Id}",
                Resource = JsonNode.Parse(record.ResourceJson),   // the stored JSON, unchanged
                Search = new BundleEntrySearch { Mode = "match" }
            });
        }
        return bundle;
    }

    /// <summary>
    /// Reads the id from a qr-context extension:
    /// "Coverage/cov-1" when isCoverage is true, otherwise the order ("DeviceRequest/ord-cpap" -> "ord-cpap").
    /// </summary>
    private static string? FindContextId(QuestionnaireResponse form, bool isCoverage)
    {
        foreach (var extension in form.Extension.Where(e => e.Url == DtrExtensions.QrContext))
        {
            var reference = extension.ValueReference;
            if (reference?.GetId() == null) continue;

            var refIsCoverage = reference.GetResourceType() == "Coverage";
            if (refIsCoverage == isCoverage) return reference.GetId();
        }
        return null;
    }
}
