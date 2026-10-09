using System.Collections.Concurrent;

namespace FHIRAPI.Services;

/// <summary>
/// Remembers which patient each coverage id belongs to.
///
/// Why: the EHR has no "GET coverage by id" API, only "GET insurances of a patient". So to answer
/// GET /fhir/r4/Coverage/{id} we must know the patient first. We learn the pair whenever we read an order
/// (order.coverageId) or search a patient's insurances. In the normal flow the Payer Gateway always reads the
/// order or searches Coverage before it reads a Coverage by id, so the pair is known.
/// Registered as a singleton.
/// </summary>
public class CoveragePatientIndex
{
    private readonly ConcurrentDictionary<string, string> _patientByCoverage = new();

    /// <summary>Records that the coverage belongs to the patient.</summary>
    public void Remember(string? coverageId, string? patientId)
    {
        if (!string.IsNullOrEmpty(coverageId) && !string.IsNullOrEmpty(patientId))
            _patientByCoverage[coverageId] = patientId;
    }

    /// <summary>The patient of a coverage, or null if not seen yet.</summary>
    public string? FindPatient(string coverageId) =>
        _patientByCoverage.TryGetValue(coverageId, out var patientId) ? patientId : null;
}
