using System.Text.Json.Nodes;
using FHIRAPI.Constants;
using FHIRAPI.Exceptions;
using FHIRAPI.Helpers;
using FHIRAPI.Middleware;
using FHIRAPI.Models.Security;

namespace FHIRAPI.Services;

/// <summary>
/// Makes sure a patient token (a SMART app token or a CRD fhirAuthorization token) only reaches its own patient:
///   - Patient: the id must be the token's patient
///   - other resources: subject / patient / beneficiary must be Patient/{token patient} when the resource has one
///     (Practitioner, Organization, Location ... have none and are allowed by scope alone)
///   - a search Bundle: every entry is checked, and included resources also need a read or search scope
/// No token on the request (checks off, or the Payer Gateway with its key) = nothing to check.
/// Breaking a rule throws FhirException 403 (OperationOutcome "forbidden").
/// </summary>
public static class PatientAccessGuard
{
    /// <summary>The token grant of this request (set by AccessTokenMiddleware), or null.</summary>
    public static TokenGrant? Grant(HttpContext context)
    {
        object? value;
        if (context.Items.TryGetValue(AccessTokenMiddleware.GrantItemKey, out value))
        {
            return value as TokenGrant;
        }
        return null;
    }

    /// <summary>The patient the token is limited to, or null when there is no limit.</summary>
    public static string? TokenPatient(HttpContext context)
    {
        TokenGrant? grant = Grant(context);
        if (grant == null)
        {
            return null;
        }
        return grant.PatientId;
    }

    /// <summary>Checks a FHIR model object (a resource or a search Bundle) before it is returned.</summary>
    public static void CheckResult(HttpContext context, object resource)
    {
        if (TokenPatient(context) == null)
        {
            return;
        }
        JsonNode? node = JsonNode.Parse(FhirJson.Serialize(resource));
        CheckJson(context, node);
    }

    /// <summary>Checks FHIR JSON (a resource or a search Bundle): read, saved or about to be saved.</summary>
    public static void CheckJson(HttpContext context, JsonNode? node)
    {
        TokenGrant? grant = Grant(context);
        if (grant == null || grant.PatientId == null || node == null)
        {
            return;
        }
        if (Text(node, "resourceType") == "Bundle")
        {
            JsonArray? entries = node["entry"] as JsonArray;
            if (entries == null)
            {
                return;
            }
            foreach (JsonNode? entry in entries)
            {
                if (entry == null)
                {
                    continue;
                }
                JsonNode? entryResource = entry["resource"];
                CheckIncludedScope(grant, entryResource);
                CheckOne(grant, entryResource);
            }
            return;
        }
        CheckOne(grant, node);
    }

    /// <summary>A search by patient: the patient parameter must be the token's patient.</summary>
    public static void CheckPatientParameter(HttpContext context, string? patient)
    {
        string? tokenPatient = TokenPatient(context);
        if (tokenPatient == null)
        {
            return;
        }
        if (patient == null || LastId(patient) != tokenPatient)
        {
            throw Forbidden();
        }
    }

    // ================================================================== helpers

    /// <summary>One resource against the token's patient.</summary>
    private static void CheckOne(TokenGrant grant, JsonNode? resource)
    {
        if (resource == null)
        {
            return;
        }
        string? type = Text(resource, "resourceType");
        if (type == "Patient")
        {
            if (Text(resource, "id") != grant.PatientId)
            {
                throw Forbidden();
            }
            return;
        }
        string[] patientFields = { "subject", "patient", "beneficiary" };
        foreach (string field in patientFields)
        {
            string? reference = ReferenceOf(resource[field]);
            if (reference == null)
            {
                continue;
            }
            if (!reference.Contains("Patient/") || LastId(reference) != grant.PatientId)
            {
                throw Forbidden();
            }
        }
    }

    /// <summary>Resources added by _include need a read or search scope for their own type.</summary>
    private static void CheckIncludedScope(TokenGrant grant, JsonNode? resource)
    {
        if (resource == null)
        {
            return;
        }
        string? type = Text(resource, "resourceType");
        if (type == null || type == "OperationOutcome")
        {
            return;
        }
        if (!SmartScopeChecker.Allows(grant.Scope, type, 'r') && !SmartScopeChecker.Allows(grant.Scope, type, 's'))
        {
            throw new FhirException(403, IssueTypes.Forbidden, "The token's scope does not allow reading " + type + ".");
        }
    }

    /// <summary>{"reference": "Patient/pat-1"} -> "Patient/pat-1"; a field that is not a reference -> null.</summary>
    private static string? ReferenceOf(JsonNode? field)
    {
        if (field is JsonObject reference)
        {
            return Text(reference, "reference");
        }
        return null;
    }

    /// <summary>"Patient/pat-1", "https://x/fhir/r4/Patient/pat-1" or "pat-1" -> "pat-1".</summary>
    private static string LastId(string reference)
    {
        string trimmed = reference.TrimEnd('/');
        int slash = trimmed.LastIndexOf('/');
        if (slash < 0)
        {
            return trimmed;
        }
        return trimmed.Substring(slash + 1);
    }

    /// <summary>A text property, or null.</summary>
    private static string? Text(JsonNode node, string property)
    {
        if (node is JsonObject obj)
        {
            JsonNode? value = obj[property];
            if (value is JsonValue text && text.TryGetValue(out string? result))
            {
                return result;
            }
        }
        return null;
    }

    /// <summary>The 403 for data of another patient.</summary>
    private static FhirException Forbidden()
    {
        return new FhirException(403, IssueTypes.Forbidden, "The access token only allows data of its own patient.");
    }
}
