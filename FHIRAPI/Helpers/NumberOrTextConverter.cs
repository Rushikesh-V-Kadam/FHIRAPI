using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FHIRAPI.Helpers;

// CLIENT-EHR: the client's EHR sends ids as numbers ("LocationID": 1383, "OrganizationID": 1383), while the FHIR API
// keeps every id as text (FHIR ids are strings). This converter lets a string property read either form.
/// <summary>
/// Reads a JSON string or a JSON number into a string property ("1383" and 1383 both become "1383").
/// Writes the value as a normal JSON string. A JSON null stays null (the converter is not called for null).
/// </summary>
public class NumberOrTextConverter : JsonConverter<string>
{
    /// <summary>Reads a string, a number (kept exactly as sent, e.g. 1383 or 12.50) or true / false as text.</summary>
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        if (reader.TokenType == JsonTokenType.Number)
        {
            // The number as it was written in the body, so no digits are lost or reformatted.
            if (reader.HasValueSequence)
            {
                return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
            }
            return Encoding.UTF8.GetString(reader.ValueSpan);
        }
        if (reader.TokenType == JsonTokenType.True)
        {
            return bool.TrueString.ToLower(CultureInfo.InvariantCulture);
        }
        if (reader.TokenType == JsonTokenType.False)
        {
            return bool.FalseString.ToLower(CultureInfo.InvariantCulture);
        }
        throw new JsonException("Expected a text or a number but found " + reader.TokenType + ".");
    }

    /// <summary>Writes the value as a JSON string.</summary>
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
