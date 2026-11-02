using Microsoft.AspNetCore.Mvc;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Models.Fhir;
using FHIRAPI.Models.Requests;
using FHIRAPI.Services;

namespace FHIRAPI.Controllers;

/// <summary>
/// Shared read and search code of the per-resource FHIR controllers (FhirAdministrativeController,
/// FhirOrderController, FhirClinicalController). Each of them has one action per resource type and calls these two
/// methods, so the read and search rules stay in FhirReadService and FhirSearchService.
/// With a patient access token, PatientAccessGuard checks every result belongs to the token's patient.
/// </summary>
public abstract class FhirResourceControllerBase : ControllerBase
{
    private readonly FhirReadService _reader;
    private readonly FhirSearchService _searcher;

    /// <summary>Called by the derived controllers.</summary>
    protected FhirResourceControllerBase(FhirReadService reader, FhirSearchService searcher)
    {
        _reader = reader;
        _searcher = searcher;
    }

    /// <summary>Reads one resource; 404 with an OperationOutcome when the EHR has no such record.</summary>
    protected async Task<IActionResult> ReadResourceAsync(string type, string id, CancellationToken ct)
    {
        // Binary has no patient: with a patient token, its DocumentReference (same id) is checked first
        if (type == "Binary" && PatientAccessGuard.TokenPatient(HttpContext) != null)
        {
            Resource? document = await _reader.ReadAsync("DocumentReference", id, ct);
            if (document != null)
            {
                PatientAccessGuard.CheckResult(HttpContext, document);
            }
        }

        Resource? resource = await _reader.ReadAsync(type, id, ct);
        if (resource == null)
        {
            throw new FhirException(404, IssueTypes.NotFound, type + "/" + id + " was not found.");
        }
        PatientAccessGuard.CheckResult(HttpContext, resource);
        return FhirResults.Ok(resource);
    }

    /// <summary>Runs a search and returns the searchset Bundle.</summary>
    protected async Task<IActionResult> SearchResourcesAsync(string type, FhirSearchParameters search, CancellationToken ct)
    {
        Bundle bundle = await _searcher.SearchAsync(type, search, ct);
        PatientAccessGuard.CheckResult(HttpContext, bundle);
        return FhirResults.Ok(bundle);
    }

    /// <summary>The _include values, or an empty list when none were sent.</summary>
    protected static List<string> IncludeList(List<string>? include)
    {
        if (include == null)
        {
            return new List<string>();
        }
        return include;
    }
}
