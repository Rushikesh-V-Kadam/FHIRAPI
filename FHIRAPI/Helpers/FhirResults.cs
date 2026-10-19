using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Constants;
using FHIRAPI.Models.Fhir;

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

    /// <summary>Error response with an OperationOutcome body.</summary>
    public static ContentResult Error(int statusCode, string issueCode, string message)
    {
        var severity = statusCode >= 500 ? "fatal" : "error";
        return Ok(OperationOutcome.Create(severity, issueCode, message), statusCode);
    }
}
