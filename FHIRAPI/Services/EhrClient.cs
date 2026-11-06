using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using FHIRAPI.Helpers;
using FHIRAPI.Repositories.Database;
using FHIRAPI.Exceptions;
using FHIRAPI.Models.Ehr;

namespace FHIRAPI.Services;

/// <summary>
/// HttpClient-based implementation of IEhrClient.
/// The base address, timeout and API key are configured in Program.cs from the "Ehr" settings.
/// </summary>
public class EhrClient : IEhrClient
{
    /// <summary>
    /// JSON settings for the EHR: property names are matched without case (the client sends LocationID, NPI, LINE1 ...,
    /// the mock EHR sends camelCase) and ids sent as numbers are read as text.
    /// </summary>
    private static readonly JsonSerializerOptions Json = CreateJsonOptions();

    private readonly HttpClient _http;

    /// <summary>Created by the HttpClient factory.</summary>
    public EhrClient(HttpClient http)
    {
        _http = http;
    }

    // ------------------------------------------------------------------ reads

    public Task<EhrPatientDto?> GetPatientAsync(string patientId, CancellationToken ct) =>
        GetAsync<EhrPatientDto>($"api/patients/{Esc(patientId)}", ct);

    public Task<List<EhrInsuranceDto>> GetInsurancesAsync(string patientId, string? status, CancellationToken ct) =>
        GetListAsync<EhrInsuranceDto>($"api/patients/{Esc(patientId)}/insurances{Query(("status", status))}", ct);

    // CLIENT-EHR: the EHR sends its own unique coverage id; one insurance is read by it.
    // TODO: path of the client's "insurance by coverage id" API (the mock EHR serves api/insurances/{coverageId}).
    public Task<EhrInsuranceDto?> GetInsuranceAsync(string coverageId, CancellationToken ct) =>
        GetAsync<EhrInsuranceDto>($"api/insurances/{Esc(coverageId)}", ct);

    public Task<EhrPractitionerDto?> GetPractitionerAsync(string practitionerId, CancellationToken ct) =>
        GetAsync<EhrPractitionerDto>($"api/practitioners/{Esc(practitionerId)}", ct);

    public Task<EhrOrganizationDto?> GetOrganizationAsync(string organizationId, CancellationToken ct) =>
        GetAsync<EhrOrganizationDto>($"api/organizations/{Esc(organizationId)}", ct);

    public Task<EhrLocationDto?> GetLocationAsync(string locationId, CancellationToken ct) =>
        GetAsync<EhrLocationDto>($"api/locations/{Esc(locationId)}", ct);

    public Task<EhrOrderDto?> GetOrderAsync(string orderId, CancellationToken ct) =>
        GetAsync<EhrOrderDto>($"api/orders/{Esc(orderId)}", ct);

    public Task<EhrEncounterDto?> GetEncounterAsync(string encounterId, CancellationToken ct) =>
        GetAsync<EhrEncounterDto>($"api/encounters/{Esc(encounterId)}", ct);

    public Task<List<EhrEncounterDto>> GetEncountersAsync(string patientId, string? from, CancellationToken ct) =>
        GetListAsync<EhrEncounterDto>($"api/patients/{Esc(patientId)}/encounters{Query(("from", from))}", ct);

    // CLIENT-EHR: the client's list APIs take only patientID and return every row of the patient; the FHIR search
    // filters (code, date, category, clinical-status) are applied in FhirSearchService.
    // TODO: paths of the client's list APIs as given by the client; they may change when the client publishes its API spec.

    /// <summary>Lab results of a patient: GET Patients/getobslst?patientID={patientId} (entity ObservationDetailList).</summary>
    public Task<List<EhrObservationDto>> GetObservationsAsync(string patientId, CancellationToken ct)
    {
        return GetListAsync<EhrObservationDto>("Patients/getobslst?patientID=" + Esc(patientId), ct);
    }

    /// <summary>Diagnoses of a patient: GET Patients/getconlst?patientID={patientId} (entity ConditionDetailList).</summary>
    public Task<List<EhrConditionDto>> GetConditionsAsync(string patientId, CancellationToken ct)
    {
        return GetListAsync<EhrConditionDto>("Patients/getconlst?patientID=" + Esc(patientId), ct);
    }

    /// <summary>Procedures of a patient: GET Patients/getproclst?patientID={patientId} (entity ProcedureDetailList).</summary>
    public Task<List<EhrProcedureDto>> GetProceduresAsync(string patientId, CancellationToken ct)
    {
        return GetListAsync<EhrProcedureDto>("Patients/getproclst?patientID=" + Esc(patientId), ct);
    }

    public Task<List<EhrDocumentDto>> GetDocumentsAsync(string patientId, string? typeCode, CancellationToken ct) =>
        GetListAsync<EhrDocumentDto>($"api/patients/{Esc(patientId)}/documents{Query(("typeCode", typeCode))}", ct);

    public Task<EhrDocumentDto?> GetDocumentAsync(string documentId, CancellationToken ct) =>
        GetAsync<EhrDocumentDto>($"api/documents/{Esc(documentId)}", ct);

    public Task<EhrDocumentContentDto?> GetDocumentContentAsync(string documentId, CancellationToken ct) =>
        GetAsync<EhrDocumentContentDto>($"api/documents/{Esc(documentId)}/content", ct);

    // ------------------------------------------------------------------ write-back

    public async Task<EhrQuestionnaireResponseDto> SaveQuestionnaireResponseAsync(EhrQuestionnaireResponseDto form, CancellationToken ct)
    {
        var isNew = string.IsNullOrEmpty(form.ResponseId);
        var path = isNew
            ? $"api/patients/{Esc(form.PatientId)}/questionnaire-responses"
            : $"api/patients/{Esc(form.PatientId)}/questionnaire-responses/{Esc(form.ResponseId)}";

        try
        {
            using var response = isNew
                ? await _http.PostAsJsonAsync(path, form, Json, ct)
                : await _http.PutAsJsonAsync(path, form, Json, ct);

            if (!response.IsSuccessStatusCode)
                throw new EhrException($"EHR {(isNew ? "POST" : "PUT")} {path} returned {(int)response.StatusCode}");

            // The EHR returns its copy with the ResponseId it assigned; keep ours if the body is empty
            var saved = await ReadBodyAsync<EhrQuestionnaireResponseDto>(response, ct);
            return saved != null && !string.IsNullOrEmpty(saved.ResponseId) ? saved : form;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new EhrException($"EHR {path} could not be reached: {ex.Message}", ex);
        }
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Web defaults (names without case) plus ids that may come as numbers.</summary>
    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        // CLIENT-EHR: the client's EHR sends ids as numbers, e.g. "LocationID": 1383 and "OrganizationID": 1383.
        options.Converters.Add(new NumberOrTextConverter());
        return options;
    }

    /// <summary>GET that returns one object, or null on 404 or when the EHR answers with a null entity.</summary>
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        JsonNode? entity = await GetEntityAsync(path, ct);
        if (entity == null)
        {
            return null;
        }
        return Deserialize<T>(entity, path);
    }

    /// <summary>
    /// GET that returns a list; an unknown patient gives an empty list. The list may come as the entity itself
    /// (the client's format) or as { items: [...] } (the mock EHR).
    /// </summary>
    private async Task<List<T>> GetListAsync<T>(string path, CancellationToken ct)
    {
        JsonNode? entity = await GetEntityAsync(path, ct);
        if (entity == null)
        {
            return new List<T>();
        }
        // CLIENT-EHR: the client's EHR sends the rows as the entity: { "<EntityName>": [ ... ], "TOKEN_EXPIRED": "", ... }
        if (entity is JsonArray)
        {
            List<T>? rows = Deserialize<List<T>>(entity, path);
            if (rows == null)
            {
                return new List<T>();
            }
            return rows;
        }
        EhrList<T>? list = Deserialize<EhrList<T>>(entity, path);
        if (list == null || list.Items == null)
        {
            return new List<T>();
        }
        return list.Items;
    }

    /// <summary>
    /// Sends the GET and returns the entity of the answer: null on 404 or an empty body. The client's format
    /// { "LocationDetail": {...}, "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" } is unwrapped (see EhrResponse); a filled
    /// TOKEN_EXPIRED or ERROR_MESSAGE is an EhrException. A body without that format is the data itself.
    /// </summary>
    private async Task<JsonNode?> GetEntityAsync(string path, CancellationToken ct)
    {
        string text;
        try
        {
            using (HttpResponseMessage response = await _http.GetAsync(path, ct))
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
                if (!response.IsSuccessStatusCode)
                {
                    throw new EhrException($"EHR GET {path} returned {(int)response.StatusCode}");
                }
                text = await response.Content.ReadAsStringAsync(ct);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new EhrException($"EHR GET {path} failed: {ex.Message}", ex);
        }
        // CLIENT-EHR: every client EHR answer is wrapped as { "<EntityName>": ..., "TOKEN_EXPIRED": "", "ERROR_MESSAGE": "" }.
        return EhrResponse.Unwrap(text, "EHR GET " + path);
    }

    /// <summary>Turns the entity into T; a body that does not fit T is an EhrException.</summary>
    private static T? Deserialize<T>(JsonNode entity, string path) where T : class
    {
        try
        {
            return entity.Deserialize<T>(Json);
        }
        catch (JsonException ex)
        {
            throw new EhrException($"EHR GET {path} returned data that could not be read: {ex.Message}", ex);
        }
    }

    /// <summary>Reads a JSON body (unwrapping the client's format); returns null when the body or the entity is empty.</summary>
    private static async Task<T?> ReadBodyAsync<T>(HttpResponseMessage response, CancellationToken ct) where T : class
    {
        string text = await response.Content.ReadAsStringAsync(ct);
        JsonNode? entity = EhrResponse.Unwrap(text, "EHR " + response.RequestMessage?.Method + " " + response.RequestMessage?.RequestUri?.AbsolutePath);
        if (entity == null)
        {
            return null;
        }
        return Deserialize<T>(entity, response.RequestMessage?.RequestUri?.AbsolutePath ?? string.Empty);
    }

    /// <summary>URL-encodes a path segment.</summary>
    private static string Esc(string value) => Uri.EscapeDataString(value);

    /// <summary>Builds "?a=1&amp;b=2" from the non-empty values.</summary>
    private static string Query(params (string Name, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Name}={Esc(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }
}
