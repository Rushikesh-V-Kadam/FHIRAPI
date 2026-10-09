using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FHIRAPI.Exceptions;
using FHIRAPI.Models.Ehr;

namespace FHIRAPI.Services;

/// <summary>
/// HttpClient-based implementation of IEhrClient.
/// The base address, timeout and API key are configured in Program.cs from the "Ehr" settings.
/// </summary>
public class EhrClient : IEhrClient
{
    /// <summary>The EHR uses camelCase JSON.</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

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

    public Task<EhrPractitionerDto?> GetPractitionerAsync(string practitionerId, CancellationToken ct) =>
        GetAsync<EhrPractitionerDto>($"api/practitioners/{Esc(practitionerId)}", ct);

    public Task<EhrOrganizationDto?> GetOrganizationAsync(string organizationId, CancellationToken ct) =>
        GetAsync<EhrOrganizationDto>($"api/organizations/{Esc(organizationId)}", ct);

    public Task<EhrLocationDto?> GetLocationAsync(string locationId, CancellationToken ct) =>
        GetAsync<EhrLocationDto>($"api/locations/{Esc(locationId)}", ct);

    public Task<EhrOrderDto?> GetOrderAsync(string orderId, CancellationToken ct) =>
        GetAsync<EhrOrderDto>($"api/orders/{Esc(orderId)}", ct);

    public Task<EhrAppointmentDto?> GetAppointmentAsync(string appointmentId, CancellationToken ct) =>
        GetAsync<EhrAppointmentDto>($"api/appointments/{Esc(appointmentId)}", ct);

    public Task<EhrEncounterDto?> GetEncounterAsync(string encounterId, CancellationToken ct) =>
        GetAsync<EhrEncounterDto>($"api/encounters/{Esc(encounterId)}", ct);

    public Task<List<EhrEncounterDto>> GetEncountersAsync(string patientId, string? from, CancellationToken ct) =>
        GetListAsync<EhrEncounterDto>($"api/patients/{Esc(patientId)}/encounters{Query(("from", from))}", ct);

    public Task<List<EhrObservationDto>> GetObservationsAsync(string patientId, string? codes, string? category, string? from, CancellationToken ct) =>
        GetListAsync<EhrObservationDto>(
            $"api/patients/{Esc(patientId)}/observations{Query(("codes", codes), ("category", category), ("from", from))}", ct);

    public Task<List<EhrConditionDto>> GetConditionsAsync(string patientId, string? codes, string? status, CancellationToken ct) =>
        GetListAsync<EhrConditionDto>($"api/patients/{Esc(patientId)}/conditions{Query(("codes", codes), ("status", status))}", ct);

    public Task<List<EhrProcedureDto>> GetProceduresAsync(string patientId, string? codes, string? from, CancellationToken ct) =>
        GetListAsync<EhrProcedureDto>($"api/patients/{Esc(patientId)}/procedures{Query(("codes", codes), ("from", from))}", ct);

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

    /// <summary>GET that returns one object, or null on 404.</summary>
    private async Task<T?> GetAsync<T>(string path, CancellationToken ct) where T : class
    {
        try
        {
            using var response = await _http.GetAsync(path, ct);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
                throw new EhrException($"EHR GET {path} returned {(int)response.StatusCode}");
            return await ReadBodyAsync<T>(response, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new EhrException($"EHR GET {path} failed: {ex.Message}", ex);
        }
    }

    /// <summary>GET that returns a list ({ items: [...] }); an unknown patient gives an empty list.</summary>
    private async Task<List<T>> GetListAsync<T>(string path, CancellationToken ct)
    {
        var list = await GetAsync<EhrList<T>>(path, ct);
        return list?.Items ?? new List<T>();
    }

    /// <summary>Reads a JSON body; returns null when the body is empty.</summary>
    private static async Task<T?> ReadBodyAsync<T>(HttpResponseMessage response, CancellationToken ct) where T : class
    {
        var text = await response.Content.ReadAsStringAsync(ct);
        return string.IsNullOrWhiteSpace(text) ? null : JsonSerializer.Deserialize<T>(text, Json);
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
