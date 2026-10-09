namespace MockEhr.Api.Models;

/// <summary>
/// A list answer of the EHR read APIs: { "items": [ ... ], "total": n }. Plain JSON (camelCase), not FHIR.
/// An empty list is { "items": [], "total": 0 } with status 200 (not 404).
/// </summary>
public class EhrList<T>
{
    /// <summary>The rows.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Number of rows in items.</summary>
    /// <example>1</example>
    public int Total { get; set; }
}
