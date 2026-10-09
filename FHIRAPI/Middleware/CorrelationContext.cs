namespace FHIRAPI.Middleware;

/// <summary>
/// Holds the correlation id of the request currently being processed.
/// AsyncLocal keeps the value separate for each request, even when many run in parallel.
/// </summary>
public static class CorrelationContext
{
    private static readonly AsyncLocal<string?> Current = new();

    /// <summary>The current correlation id (a new one is created if none was set).</summary>
    public static string Id
    {
        get => Current.Value ??= Guid.NewGuid().ToString("N");
        set => Current.Value = value;
    }
}
