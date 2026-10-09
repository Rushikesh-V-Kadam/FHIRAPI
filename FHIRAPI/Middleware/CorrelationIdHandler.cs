using FHIRAPI.Constants;

namespace FHIRAPI.Middleware;

/// <summary>
/// Adds the current correlation id to every outgoing call to the EHR.
/// Registered on the EHR HttpClient in Program.cs.
/// </summary>
public class CorrelationIdHandler : DelegatingHandler
{
    /// <summary>Adds the header, then sends the request.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Remove(ApiHeaders.CorrelationId);
        request.Headers.Add(ApiHeaders.CorrelationId, CorrelationContext.Id);
        return base.SendAsync(request, cancellationToken);
    }
}
