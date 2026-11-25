using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Repositories.Interfaces;

namespace FHIRAPI.Repositories.Database;

/// <summary>
/// Stores forms in the EHR's Oracle database (table PA_QUESTIONNAIRE_RESPONSE) through the two REST APIs the client
/// EHR team provides. This API has no database connection of its own.
///   GET  {Ehr:BaseUrl}/{Ehr:QuestionnaireGetPath}?responseID={id}   one form   (default Enterprise/getpasquestdtl)
///   POST {Ehr:BaseUrl}/{Ehr:QuestionnaireSavePath}                  insert or update by ResponseID (default Enterprise/savepasquest)
/// Both use the client's property names, which QuestionnaireResponseRecord carries itself ([JsonPropertyName]: ResponseID,
/// PatientID, ResourceJSON ...), so the record is sent and read as it is.
/// Every answer has the client's format { "QuestionnaireDetail": row or null, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
/// (EhrResponse); QuestionnaireDetail null = no such form.
/// Selected with "Storage": { "QuestionnaireResponseStore": "EhrApi" }.
/// </summary>
public class EhrApiQuestionnaireResponseRepository : IQuestionnaireResponseRepository
{
    /// <summary>JSON settings for the client's rows (property names come from [JsonPropertyName] on QuestionnaireResponseRecord).</summary>
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpFactory;
    private readonly EhrSettings _settings;

    /// <summary>Created by dependency injection.</summary>
    public EhrApiQuestionnaireResponseRepository(IHttpClientFactory httpFactory, IOptions<EhrSettings> settings)
    {
        _httpFactory = httpFactory;
        _settings = settings.Value;
    }

    /// <inheritdoc />
    public async Task<QuestionnaireResponseRecord?> GetAsync(string id, CancellationToken ct)
    {
        string path = _settings.QuestionnaireGetPath.Trim('/') + "?responseID=" + Uri.EscapeDataString(id);
        string? text = await SendAsync(HttpMethod.Get, path, null, true, ct);
        JsonNode? entity = EhrResponse.Unwrap(text, "EHR GET " + _settings.QuestionnaireGetPath);
        if (entity == null)
        {
            return null;   // QuestionnaireDetail null: no form with this id
        }

        QuestionnaireResponseRecord? record = Read<QuestionnaireResponseRecord>(entity);
        if (record == null)
        {
            return null;
        }
        if (string.IsNullOrEmpty(record.ResourceJson))
        {
            // Without the stored FHIR JSON there is nothing to return to the caller.
            throw new EhrException("EHR GET " + _settings.QuestionnaireGetPath + " returned form '" + id + "' without ResourceJSON.");
        }
        return record;
    }

    /// <inheritdoc />
    public Task<List<QuestionnaireResponseRecord>> GetByPatientAsync(string patientId, CancellationToken ct)
    {
        // CLIENT-EHR: the client provides only "get by responseID" and "save"; there is no "forms of a patient" API.
        // TODO: when the client EHR team adds a list API (e.g. by PatientID / OrderID), call it here.
        throw new FhirException(501, IssueTypes.NotSupported,
            "QuestionnaireResponse search by patient is not supported yet: the EHR has no API that lists the forms of a patient. Read a form by its id instead.");
    }

    /// <inheritdoc />
    public async Task SaveAsync(QuestionnaireResponseRecord record, CancellationToken ct)
    {
        record.LastUpdatedUtc = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(record, Json);
        string? answer = await SendAsync(HttpMethod.Post, _settings.QuestionnaireSavePath.Trim('/'), json, false, ct);

        // TODO: confirm with the EHR team what savepasquest answers; for now only TOKEN_EXPIRED / ERROR_MESSAGE are checked
        // (EhrResponse.Unwrap throws when one of them is filled) and the rest of the answer is not used.
        EhrResponse.Unwrap(answer, "EHR POST " + _settings.QuestionnaireSavePath);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Sends one call with the timeout and returns the body. Returns null for 404 when
    /// <paramref name="notFoundIsNull"/> is true; every other problem is an EhrException (502).
    /// </summary>
    private async Task<string?> SendAsync(HttpMethod method, string path, string? json, bool notFoundIsNull, CancellationToken ct)
    {
        string url = _settings.BaseUrl.TrimEnd('/') + "/" + path;
        string label = "EHR " + method.Method + " " + path;

        using (HttpRequestMessage request = new HttpRequestMessage(method, url))
        using (CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            if (json != null)
            {
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _settings.TimeoutSeconds)));

            try
            {
                HttpClient http = _httpFactory.CreateClient();
                using (HttpResponseMessage response = await http.SendAsync(request, timeout.Token))
                {
                    string text = await response.Content.ReadAsStringAsync(timeout.Token);
                    int status = (int)response.StatusCode;
                    if (status == 404 && notFoundIsNull)
                    {
                        return null;
                    }
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new EhrException(label + " returned HTTP " + status);
                    }
                    return text;
                }
            }
            catch (OperationCanceledException ex)
            {
                if (ct.IsCancellationRequested)
                {
                    throw;   // the caller is gone
                }
                throw new EhrException(label + " did not answer within " + _settings.TimeoutSeconds + " seconds.", ex);
            }
            catch (HttpRequestException ex)
            {
                throw new EhrException(label + " failed: " + ex.Message, ex);
            }
        }
    }

    /// <summary>The entity -> object; an entity that does not have the expected shape becomes EhrException.</summary>
    private static T? Read<T>(JsonNode entity)
    {
        try
        {
            return entity.Deserialize<T>(Json);
        }
        catch (JsonException ex)
        {
            throw new EhrException("The EHR data API returned a body that could not be read: " + ex.Message, ex);
        }
    }
}
