using System.Collections.Concurrent;
using FHIRAPI.Repositories.Interfaces;

namespace FHIRAPI.Repositories.InMemory;

/// <summary>
/// Keeps forms in memory. For local testing only: everything is lost when the API restarts.
/// Selected with "Storage": { "QuestionnaireResponseStore": "InMemory" }.
/// </summary>
public class InMemoryQuestionnaireResponseRepository : IQuestionnaireResponseRepository
{
    private readonly ConcurrentDictionary<string, QuestionnaireResponseRecord> _rows = new();

    /// <inheritdoc />
    public Task<QuestionnaireResponseRecord?> GetAsync(string id, CancellationToken ct) =>
        Task.FromResult(_rows.TryGetValue(id, out var row) ? row : null);

    /// <inheritdoc />
    public Task<List<QuestionnaireResponseRecord>> GetByPatientAsync(string patientId, CancellationToken ct)
    {
        var rows = _rows.Values
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.LastUpdatedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <inheritdoc />
    public Task SaveAsync(QuestionnaireResponseRecord record, CancellationToken ct)
    {
        record.LastUpdatedUtc = DateTime.UtcNow;
        _rows[record.Id] = record;
        return Task.CompletedTask;
    }
}
