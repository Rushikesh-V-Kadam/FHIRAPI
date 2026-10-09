using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace FHIRAPI.Helpers;

/// <summary>
/// JSON settings for FHIR:
///  - camelCase property names (FHIR JSON style)
///  - null values are not written
///  - empty lists are not written (FHIR does not allow empty arrays)
/// </summary>
public static class FhirJson
{
    /// <summary>Serializer options used for every FHIR body.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { SkipEmptyLists } }
    };

    /// <summary>Writes any FHIR model (or JsonNode) as JSON. The runtime type is used, so subclasses are written fully.</summary>
    public static string Serialize(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);

    /// <summary>Reads JSON into a FHIR model class. Unknown JSON properties are ignored.</summary>
    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>Tells the serializer to skip list properties that have no items.</summary>
    private static void SkipEmptyLists(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        foreach (var property in typeInfo.Properties)
        {
            if (property.PropertyType == typeof(string)) continue;
            if (typeof(ICollection).IsAssignableFrom(property.PropertyType))
                property.ShouldSerialize = (_, value) => value is ICollection list && list.Count > 0;
        }
    }
}
