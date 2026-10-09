using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Configuration;
using FHIRAPI.Constants;
using FHIRAPI.Models.Fhir;
using FHIRAPI.Versioning;

namespace FHIRAPI.Helpers;

/// <summary>
/// Builds HTTP responses with a FHIR JSON body and the application/fhir+json content type.
/// </summary>
public static class FhirResults
{
    /// <summary>200 (or the given status) with a FHIR body.</summary>
    public static ContentResult Ok(object resource, int statusCode = 200) => new()
    {
        Content = FhirJson.Serialize(resource),
        ContentType = FhirMediaTypes.FhirJson,
        StatusCode = statusCode
    };

    /// <summary>
    /// 200 (or the given status) with a FHIR body in the caller's release: unchanged for R4,
    /// converted by R5Converter (with the R5 content type) for R5.
    /// </summary>
    public static ContentResult Ok(object resource, FhirRelease release, FhirServerSettings server, int statusCode = 200)
    {
        if (!release.IsR5())
        {
            return Ok(resource, statusCode);
        }
        ContentResult result = Ok(R5Converter.ToR5(resource, server.BaseUrl, server.BaseUrlR5), statusCode);
        result.ContentType = release.ContentType();
        return result;
    }

    /// <summary>Error response with an OperationOutcome body.</summary>
    public static ContentResult Error(int statusCode, string issueCode, string message)
    {
        var severity = statusCode >= 500 ? "fatal" : "error";
        return Ok(OperationOutcome.Create(severity, issueCode, message), statusCode);
    }
}
