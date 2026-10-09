using FHIRAPI.Configuration;
using FHIRAPI.Constants;

namespace FHIRAPI.Versioning;

/// <summary>
/// The FHIR release a caller asked for, taken from the URL:
///   /fhir/r4/...  ->  R4 (4.0.1)  the main API: what the Payer Gateway, the Da Vinci guides and today's payers use
///   /fhir/r5/...  ->  R5 (5.0.0)  the same data, converted to R5 on the way out (R5Converter)
/// Inside the API everything is R4; R5 is only a different view of the same resources.
/// </summary>
public class FhirRelease
{
    /// <summary>FHIR R4 (4.0.1).</summary>
    public static readonly FhirRelease R4 = new FhirRelease("r4", FhirVersions.Fhir);

    /// <summary>FHIR R5 (5.0.0).</summary>
    public static readonly FhirRelease R5 = new FhirRelease("r5", FhirVersions.FhirR5);

    /// <summary>The URL segment: "r4" or "r5".</summary>
    public string Name { get; }

    /// <summary>The FHIR version number: "4.0.1" or "5.0.0".</summary>
    public string FhirVersion { get; }

    private FhirRelease(string name, string fhirVersion)
    {
        Name = name;
        FhirVersion = fhirVersion;
    }

    /// <summary>True for R5.</summary>
    public bool IsR5()
    {
        return Name == "r5";
    }

    /// <summary>The release of a request: R5 when the path starts with /fhir/r5, otherwise R4.</summary>
    public static FhirRelease FromRequest(HttpRequest request)
    {
        if (request.Path.StartsWithSegments("/fhir/r5", StringComparison.OrdinalIgnoreCase))
        {
            return R5;
        }
        return R4;
    }

    /// <summary>
    /// The release named in a request body ("r4", "4.0.1", "r5", "5.0.0"; empty = R4), or null when the name is unknown.
    /// </summary>
    public static FhirRelease? FromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return R4;
        }
        string value = name.Trim().ToLowerInvariant();
        if (value == "r4" || value == FhirVersions.Fhir)
        {
            return R4;
        }
        if (value == "r5" || value == FhirVersions.FhirR5)
        {
            return R5;
        }
        return null;
    }

    /// <summary>Public base URL of this release, e.g. http://localhost:5100/fhir/r5.</summary>
    public string BaseUrl(FhirServerSettings server)
    {
        if (IsR5())
        {
            return server.BaseUrlR5;
        }
        return server.BaseUrl;
    }

    /// <summary>Content type of a FHIR body of this release ("application/fhir+json; fhirVersion=5.0" for R5).</summary>
    public string ContentType()
    {
        if (IsR5())
        {
            return FhirMediaTypes.FhirJsonR5;
        }
        return FhirMediaTypes.FhirJson;
    }
}
