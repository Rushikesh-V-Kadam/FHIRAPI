using System.Text.Json;
using System.Text.Json.Nodes;
using FHIRAPI.Constants;
using FHIRAPI.Helpers;

namespace FHIRAPI.Versioning;

/// <summary>
/// Turns the R4 resources this API builds into FHIR R5, for the /fhir/r5 endpoints.
///
/// Only the elements this API actually writes are converted (see the mappers). Everything else is the same in R4 and R5
/// and is passed through unchanged. Per resource:
///   Coverage           + kind = "insurance"; payor[0] -> insurer; subscriberId (text) -> subscriberId[] (Identifier);
///                      class.value (text) -> class.value (Identifier)
///   Organization       telecom + address -> contact[] (ExtendedContactDetail)
///   ServiceRequest     code -> code.concept; locationCode + locationReference -> location[]; reasonCode -> reason[]
///   DeviceRequest      codeCodeableConcept -> code.concept; performer -> performer.reference; reasonCode -> reason[]
///   MedicationRequest  medicationCodeableConcept -> medication.concept; reasonCode -> reason[];
///                      dispenseRequest.performer -> dispenseRequest.dispenser
///   Appointment        serviceType[] -> serviceType[].concept
///   Encounter          status codes (finished -> completed, arrived -> in-progress ...); class -> class[] (CodeableConcept);
///                      period -> actualPeriod; participant.individual -> participant.actor; reasonCode -> reason[].value[];
///                      hospitalization -> admission
///   Procedure          performedDateTime / performedPeriod -> occurrenceDateTime / occurrencePeriod
///   CapabilityStatement fhirVersion = 5.0.0, no US Core implementation guide
///   every resource     meta.profile removed (the US Core and Da Vinci CRD profiles are R4 profiles)
///   Bundle             each entry's resource converted
/// Patient, Practitioner, PractitionerRole, Location, Observation, Condition, DocumentReference, Binary,
/// QuestionnaireResponse and OperationOutcome need no change for the elements written here.
/// Finally every URL that points to the R4 base (fullUrl, Binary links, SMART URLs) is changed to the R5 base.
/// </summary>
public static class R5Converter
{
    /// <summary>R4 encounter status -> R5 encounter status (codes that are the same in both are not listed).</summary>
    private static readonly Dictionary<string, string> EncounterStatus = new Dictionary<string, string>
    {
        { "arrived", "in-progress" },
        { "triaged", "in-progress" },
        { "onleave", "on-hold" },
        { "finished", "completed" }
    };

    /// <summary>
    /// Converts a resource (a model class or a JsonNode) to R5 JSON. The input is not changed.
    /// </summary>
    /// <param name="resource">An R4 resource built by this API (Patient, Bundle ...) or raw R4 JSON.</param>
    /// <param name="r4BaseUrl">Public R4 base URL, e.g. http://localhost:5100/fhir/r4.</param>
    /// <param name="r5BaseUrl">Public R5 base URL, e.g. http://localhost:5100/fhir/r5.</param>
    public static JsonNode ToR5(object resource, string r4BaseUrl, string r5BaseUrl)
    {
        JsonNode? node;
        JsonNode? given = resource as JsonNode;
        if (given != null)
        {
            node = given.DeepClone();
        }
        else
        {
            node = JsonSerializer.SerializeToNode(resource, resource.GetType(), FhirJson.Options);
        }
        if (node == null)
        {
            return new JsonObject();
        }

        JsonObject? obj = node as JsonObject;
        if (obj != null)
        {
            ConvertResource(obj);
        }
        return RewriteUrls(node, r4BaseUrl.TrimEnd('/'), r5BaseUrl.TrimEnd('/'));
    }

    // ================================================================== per resource

    /// <summary>Converts one resource in place (and the resources inside a Bundle).</summary>
    private static void ConvertResource(JsonObject resource)
    {
        string? type = Text(resource, "resourceType");
        RemoveProfiles(resource);

        if (type == "Bundle")
        {
            JsonArray? entries = resource["entry"] as JsonArray;
            if (entries != null)
            {
                foreach (JsonNode? entry in entries)
                {
                    JsonObject? inner = entry == null ? null : entry["resource"] as JsonObject;
                    if (inner != null)
                    {
                        ConvertResource(inner);
                    }
                }
            }
        }
        else if (type == "Coverage") ConvertCoverage(resource);
        else if (type == "Organization") ConvertOrganization(resource);
        else if (type == "ServiceRequest") ConvertServiceRequest(resource);
        else if (type == "DeviceRequest") ConvertDeviceRequest(resource);
        else if (type == "MedicationRequest") ConvertMedicationRequest(resource);
        else if (type == "Appointment") ConvertAppointment(resource);
        else if (type == "Encounter") ConvertEncounter(resource);
        else if (type == "Procedure") ConvertProcedure(resource);
        else if (type == "CapabilityStatement") ConvertCapabilityStatement(resource);
    }

    /// <summary>Coverage: kind, insurer, subscriberId and class.value as R5 expects them.</summary>
    private static void ConvertCoverage(JsonObject coverage)
    {
        if (coverage["kind"] == null)
        {
            coverage["kind"] = "insurance";   // R5 requires it; every coverage this API builds is an insurance
        }

        JsonArray? payors = Take(coverage, "payor") as JsonArray;
        if (payors != null && payors.Count > 0 && payors[0] != null)
        {
            coverage["insurer"] = payors[0]!.DeepClone();   // R5 allows one insurer
        }

        JsonNode? subscriberId = Take(coverage, "subscriberId");
        if (subscriberId != null)
        {
            JsonObject identifier = new JsonObject();
            identifier["value"] = subscriberId.DeepClone();
            coverage["subscriberId"] = new JsonArray(identifier);
        }

        JsonArray? classes = coverage["class"] as JsonArray;
        if (classes != null)
        {
            foreach (JsonNode? item in classes)
            {
                JsonObject? coverageClass = item as JsonObject;
                JsonNode? value = coverageClass == null ? null : Take(coverageClass, "value");
                if (coverageClass != null && value != null)
                {
                    JsonObject identifier = new JsonObject();
                    identifier["value"] = value.DeepClone();
                    coverageClass["value"] = identifier;
                }
            }
        }
    }

    /// <summary>Organization: telecom and address move into contact[] (one contact per address).</summary>
    private static void ConvertOrganization(JsonObject organization)
    {
        JsonArray? telecom = Take(organization, "telecom") as JsonArray;
        JsonArray? addresses = Take(organization, "address") as JsonArray;
        if (telecom == null && addresses == null)
        {
            return;
        }

        JsonArray contacts = new JsonArray();
        JsonObject first = new JsonObject();
        if (telecom != null)
        {
            first["telecom"] = telecom.DeepClone();
        }
        if (addresses != null && addresses.Count > 0)
        {
            first["address"] = addresses[0]!.DeepClone();
        }
        contacts.Add(first);

        if (addresses != null)
        {
            for (int i = 1; i < addresses.Count; i++)
            {
                JsonObject more = new JsonObject();
                more["address"] = addresses[i]!.DeepClone();
                contacts.Add(more);
            }
        }
        organization["contact"] = contacts;
    }

    /// <summary>ServiceRequest: code, location and reason become CodeableReferences.</summary>
    private static void ConvertServiceRequest(JsonObject request)
    {
        WrapConcept(request, "code", "code");

        JsonArray locations = new JsonArray();
        AddConcepts(locations, Take(request, "locationCode") as JsonArray);
        AddReferences(locations, Take(request, "locationReference") as JsonArray);
        if (locations.Count > 0)
        {
            request["location"] = locations;
        }

        MoveReasons(request);
    }

    /// <summary>DeviceRequest: code and performer become CodeableReferences, reasonCode becomes reason.</summary>
    private static void ConvertDeviceRequest(JsonObject request)
    {
        WrapConcept(request, "codeCodeableConcept", "code");

        JsonNode? performer = Take(request, "performer");
        if (performer != null)
        {
            JsonObject wrapped = new JsonObject();
            wrapped["reference"] = performer.DeepClone();
            request["performer"] = wrapped;
        }

        MoveReasons(request);
    }

    /// <summary>MedicationRequest: medication becomes a CodeableReference, the pharmacy is dispenseRequest.dispenser.</summary>
    private static void ConvertMedicationRequest(JsonObject request)
    {
        WrapConcept(request, "medicationCodeableConcept", "medication");
        MoveReasons(request);

        JsonObject? dispense = request["dispenseRequest"] as JsonObject;
        JsonNode? pharmacy = dispense == null ? null : Take(dispense, "performer");
        if (dispense != null && pharmacy != null)
        {
            dispense["dispenser"] = pharmacy.DeepClone();
        }
    }

    /// <summary>Appointment: serviceType entries become CodeableReferences.</summary>
    private static void ConvertAppointment(JsonObject appointment)
    {
        JsonArray? serviceTypes = Take(appointment, "serviceType") as JsonArray;
        if (serviceTypes != null && serviceTypes.Count > 0)
        {
            JsonArray converted = new JsonArray();
            AddConcepts(converted, serviceTypes);
            appointment["serviceType"] = converted;
        }
    }

    /// <summary>Encounter: status codes, class, actualPeriod, participant.actor, reason and admission.</summary>
    private static void ConvertEncounter(JsonObject encounter)
    {
        string? status = Text(encounter, "status");
        string? r5Status;
        if (status != null && EncounterStatus.TryGetValue(status, out r5Status))
        {
            encounter["status"] = r5Status;
        }

        JsonNode? encounterClass = Take(encounter, "class");
        if (encounterClass != null)
        {
            JsonObject concept = new JsonObject();
            concept["coding"] = new JsonArray(encounterClass.DeepClone());
            encounter["class"] = new JsonArray(concept);
        }

        Rename(encounter, "period", "actualPeriod");
        Rename(encounter, "hospitalization", "admission");

        JsonArray? participants = encounter["participant"] as JsonArray;
        if (participants != null)
        {
            foreach (JsonNode? item in participants)
            {
                JsonObject? participant = item as JsonObject;
                if (participant != null)
                {
                    Rename(participant, "individual", "actor");
                }
            }
        }

        JsonArray? reasonCodes = Take(encounter, "reasonCode") as JsonArray;
        if (reasonCodes != null && reasonCodes.Count > 0)
        {
            JsonArray values = new JsonArray();
            AddConcepts(values, reasonCodes);
            JsonObject reason = new JsonObject();
            reason["value"] = values;
            encounter["reason"] = new JsonArray(reason);
        }
    }

    /// <summary>Procedure: performed[x] is occurrence[x] in R5.</summary>
    private static void ConvertProcedure(JsonObject procedure)
    {
        Rename(procedure, "performedDateTime", "occurrenceDateTime");
        Rename(procedure, "performedPeriod", "occurrencePeriod");
    }

    /// <summary>CapabilityStatement: R5 version number; US Core is an R4 guide, so it is not claimed.</summary>
    private static void ConvertCapabilityStatement(JsonObject statement)
    {
        statement["fhirVersion"] = FhirVersions.FhirR5;
        statement.Remove("implementationGuide");
    }

    // ================================================================== helpers

    /// <summary>Removes meta.profile (the profiles this API claims are R4 profiles) and an empty meta.</summary>
    private static void RemoveProfiles(JsonObject resource)
    {
        JsonObject? meta = resource["meta"] as JsonObject;
        if (meta == null)
        {
            return;
        }
        meta.Remove("profile");
        if (meta.Count == 0)
        {
            resource.Remove("meta");
        }
    }

    /// <summary>reasonCode[] (CodeableConcept) -> reason[] (CodeableReference).</summary>
    private static void MoveReasons(JsonObject resource)
    {
        JsonArray? reasonCodes = Take(resource, "reasonCode") as JsonArray;
        if (reasonCodes == null || reasonCodes.Count == 0)
        {
            return;
        }
        JsonArray reasons = new JsonArray();
        AddConcepts(reasons, reasonCodes);
        resource["reason"] = reasons;
    }

    /// <summary>A CodeableConcept property -> a CodeableReference { "concept": ... } under a new name.</summary>
    private static void WrapConcept(JsonObject resource, string from, string to)
    {
        JsonNode? concept = Take(resource, from);
        if (concept == null)
        {
            return;
        }
        JsonObject wrapped = new JsonObject();
        wrapped["concept"] = concept.DeepClone();
        resource[to] = wrapped;
    }

    /// <summary>Adds { "concept": item } for each CodeableConcept in the list.</summary>
    private static void AddConcepts(JsonArray target, JsonArray? concepts)
    {
        if (concepts == null)
        {
            return;
        }
        foreach (JsonNode? concept in concepts)
        {
            if (concept != null)
            {
                JsonObject wrapped = new JsonObject();
                wrapped["concept"] = concept.DeepClone();
                target.Add(wrapped);
            }
        }
    }

    /// <summary>Adds { "reference": item } for each Reference in the list.</summary>
    private static void AddReferences(JsonArray target, JsonArray? references)
    {
        if (references == null)
        {
            return;
        }
        foreach (JsonNode? reference in references)
        {
            if (reference != null)
            {
                JsonObject wrapped = new JsonObject();
                wrapped["reference"] = reference.DeepClone();
                target.Add(wrapped);
            }
        }
    }

    /// <summary>Renames a property (nothing happens when it is missing).</summary>
    private static void Rename(JsonObject resource, string from, string to)
    {
        JsonNode? value = Take(resource, from);
        if (value != null)
        {
            resource[to] = value;
        }
    }

    /// <summary>Removes a property and returns its value (detached, so it can be placed elsewhere), or null.</summary>
    private static JsonNode? Take(JsonObject resource, string name)
    {
        JsonNode? value;
        if (!resource.TryGetPropertyValue(name, out value))
        {
            return null;
        }
        resource.Remove(name);
        return value;
    }

    /// <summary>A string property, or null.</summary>
    private static string? Text(JsonObject resource, string name)
    {
        JsonValue? value = resource[name] as JsonValue;
        string? text;
        if (value != null && value.TryGetValue(out text))
        {
            return text;
        }
        return null;
    }

    /// <summary>
    /// Every string that starts with the R4 base URL (fullUrl, Binary links, Bundle links, SMART URLs) is changed to
    /// start with the R5 base URL, so a client that came in through /fhir/r5 stays on /fhir/r5.
    /// </summary>
    private static JsonNode RewriteUrls(JsonNode node, string r4BaseUrl, string r5BaseUrl)
    {
        JsonObject? obj = node as JsonObject;
        if (obj != null)
        {
            List<string> names = new List<string>();
            foreach (KeyValuePair<string, JsonNode?> property in obj)
            {
                names.Add(property.Key);
            }
            foreach (string name in names)
            {
                JsonNode? child = obj[name];
                if (child != null)
                {
                    JsonNode replaced = RewriteUrls(child, r4BaseUrl, r5BaseUrl);
                    if (!ReferenceEquals(replaced, child))
                    {
                        obj[name] = replaced;
                    }
                }
            }
            return obj;
        }

        JsonArray? array = node as JsonArray;
        if (array != null)
        {
            for (int i = 0; i < array.Count; i++)
            {
                JsonNode? child = array[i];
                if (child != null)
                {
                    JsonNode replaced = RewriteUrls(child, r4BaseUrl, r5BaseUrl);
                    if (!ReferenceEquals(replaced, child))
                    {
                        array[i] = replaced;
                    }
                }
            }
            return array;
        }

        JsonValue? value = node as JsonValue;
        string? text;
        if (value != null && value.TryGetValue(out text) && StartsWithBase(text, r4BaseUrl))
        {
            return JsonValue.Create(r5BaseUrl + text.Substring(r4BaseUrl.Length))!;
        }
        return node;
    }

    /// <summary>True when the text is the base URL itself or starts with it followed by "/" or "?".</summary>
    private static bool StartsWithBase(string text, string baseUrl)
    {
        if (!text.StartsWith(baseUrl, StringComparison.Ordinal))
        {
            return false;
        }
        if (text.Length == baseUrl.Length)
        {
            return true;
        }
        char next = text[baseUrl.Length];
        return next == '/' || next == '?';
    }
}
