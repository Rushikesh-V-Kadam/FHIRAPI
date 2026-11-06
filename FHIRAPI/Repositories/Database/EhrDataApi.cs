using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using FHIRAPI.Configuration;
using FHIRAPI.Exceptions;

namespace FHIRAPI.Repositories.Database;

/// <summary>
/// Calls the EHR data API ({Ehr:BaseUrl}/{Ehr:DataPath}/...) for the SMART tables. Rows are flat camelCase JSON
/// (one property per column), the same rules as the other PA_* tables (docs/EHR-Data-API.md).
///   GET    = one row (404 -> null)
///   PUT    = insert, or update when the key exists
/// Every answer has the client's format { "<EntityName>": row or null, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }
/// (EhrResponse); a filled TOKEN_EXPIRED / ERROR_MESSAGE is an EhrException.
///   DELETE = delete one row and return it (404 -> null); used to take an authorization code exactly once
/// Every other problem is an EhrException (502).
/// </summary>
public class EhrDataApi
{
    /// <summary>The data API uses camelCase JSON.</summary>
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpFactory;
    private readonly EhrSettings _settings;

    /// <summary>Created by dependency injection.</summary>
    public EhrDataApi(IHttpClientFactory httpFactory, IOptions<EhrSettings> settings)
    {
        _httpFactory = httpFactory;
        _settings = settings.Value;
    }

    /// <summary>GET one row; null when the entity is null or the API answers 404.</summary>
    public async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        string? text = await SendAsync(HttpMethod.Get, path, null, ct);
        return Read<T>(EhrResponse.Unwrap(text, "EHR data API GET " + FirstSegment(path)));
    }

    /// <summary>PUT one row (insert or update). The answer is only checked for TOKEN_EXPIRED / ERROR_MESSAGE.</summary>
    public async Task PutAsync(string path, object row, CancellationToken ct)
    {
        string json = JsonSerializer.Serialize(row, row.GetType(), Json);
        string? text = await SendAsync(HttpMethod.Put, path, json, ct);
        EhrResponse.Unwrap(text, "EHR data API PUT " + FirstSegment(path));
    }

    /// <summary>DELETE one row and return it; null when the entity is null or the API answers 404 (no such row, or already deleted).</summary>
    public async Task<T?> DeleteAsync<T>(string path, CancellationToken ct) where T : class
    {
        string? text = await SendAsync(HttpMethod.Delete, path, null, ct);
        return Read<T>(EhrResponse.Unwrap(text, "EHR data API DELETE " + FirstSegment(path)));
    }

    /// <summary>Sends one call with the timeout and returns the body; null for 404.</summary>
    private async Task<string?> SendAsync(HttpMethod method, string path, string? json, CancellationToken ct)
    {
        string url = _settings.BaseUrl.TrimEnd('/') + "/" + _settings.DataPath.Trim('/') + "/" + path;
        string label = "EHR data API " + method.Method + " " + FirstSegment(path);

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
                    if (status == 404)
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

    /// <summary>The entity -> row; null -> null; an entity that does not have the expected shape -> EhrException.</summary>
    private static T? Read<T>(JsonNode? entity) where T : class
    {
        if (entity == null)
        {
            return null;
        }
        try
        {
            return entity.Deserialize<T>(Json);
        }
        catch (JsonException ex)
        {
            throw new EhrException("The EHR data API returned a body that could not be read: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// "smart-auth-codes/{hash}" -> "smart-auth-codes": keys of these tables are token and code hashes, so they are
    /// kept out of error messages and logs.
    /// </summary>
    private static string FirstSegment(string path)
    {
        int slash = path.IndexOf('/');
        if (slash < 0)
        {
            return path;
        }
        return path.Substring(0, slash);
    }
}
