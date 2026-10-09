using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Exceptions;
using FHIRAPI.Repositories.Interfaces;

namespace FHIRAPI.Repositories.Database;

/// <summary>
/// Stores forms in the EHR's Oracle database (table PA_QUESTIONNAIRE_RESPONSE) through the EHR data API, the same
/// Web API the Payer Gateway uses for its tables (see the gateway's docs/EHR-Data-API.md). This API has no database
/// connection of its own.
///   GET {Ehr:BaseUrl}/{Ehr:DataPath}/questionnaire-responses/{id}             one form (404 = not found)
///   GET {Ehr:BaseUrl}/{Ehr:DataPath}/questionnaire-responses?patientId=...    all forms of a patient
///   PUT {Ehr:BaseUrl}/{Ehr:DataPath}/questionnaire-responses/{id}             insert or update
/// The row is the QuestionnaireResponseRecord as camelCase JSON: every property is one column.
/// Selected with "Storage": { "QuestionnaireResponseStore": "EhrApi" }.
/// </summary>
public class EhrApiQuestionnaireResponseRepository : IQuestionnaireResponseRepository
{
    /// <summary>The data API uses camelCase JSON.</summary>
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
        string? text = await SendAsync(HttpMethod.Get, "questionnaire-responses/" + Uri.EscapeDataString(id), null, true, ct);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }
        return Read<QuestionnaireResponseRecord>(text);
    }

    /// <inheritdoc />
    public async Task<List<QuestionnaireResponseRecord>> GetByPatientAsync(string patientId, CancellationToken ct)
    {
        string? text = await SendAsync(HttpMethod.Get, "questionnaire-responses?patientId=" + Uri.EscapeDataString(patientId), null, false, ct);
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<QuestionnaireResponseRecord>();
        }
        List<QuestionnaireResponseRecord>? rows = Read<List<QuestionnaireResponseRecord>>(text);
        if (rows == null)
        {
            return new List<QuestionnaireResponseRecord>();
        }
        rows.Sort(NewestFirst);
        return rows;
    }

    /// <inheritdoc />
    public async Task SaveAsync(QuestionnaireResponseRecord record, CancellationToken ct)
    {
        record.LastUpdatedUtc = DateTime.UtcNow;
        string json = JsonSerializer.Serialize(record, Json);
        await SendAsync(HttpMethod.Put, "questionnaire-responses/" + Uri.EscapeDataString(record.Id), json, false, ct);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// Sends one call with the API key and timeout and returns the body. Returns null for 404 when
    /// <paramref name="notFoundIsNull"/> is true; every other problem is an EhrException (502).
    /// </summary>
    private async Task<string?> SendAsync(HttpMethod method, string path, string? json, bool notFoundIsNull, CancellationToken ct)
    {
        string url = _settings.BaseUrl.TrimEnd('/') + "/" + _settings.DataPath.Trim('/') + "/" + path;
        string label = "EHR data API " + method.Method + " " + path;

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

    /// <summary>JSON text -> object; a body that is not the expected JSON becomes EhrException.</summary>
    private static T? Read<T>(string text)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, Json);
        }
        catch (JsonException ex)
        {
            throw new EhrException("The EHR data API returned a body that could not be read: " + ex.Message, ex);
        }
    }

    /// <summary>Sort order of GetByPatientAsync: the form changed last comes first.</summary>
    private static int NewestFirst(QuestionnaireResponseRecord a, QuestionnaireResponseRecord b)
    {
        return b.LastUpdatedUtc.CompareTo(a.LastUpdatedUtc);
    }
}
