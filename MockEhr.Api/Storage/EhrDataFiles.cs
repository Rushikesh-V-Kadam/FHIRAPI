using System.Reflection;
using System.Text.Json;

namespace MockEhr.Api.Storage;

/// <summary>
/// The mock EHR keeps every list of EhrData in its own JSON file, named after the property in kebab-case:
///   Patients -> patients.json, Insurances -> insurances.json, DocumentContent -> document-content.json,
///   WriteBacks -> write-backs.json, GatewayTables -> gateway-tables.json ...
/// The single number QrCounter goes to counters.json.
/// Used for the live data (Data folder) and for the test data the EHR starts with (Data/TestData folder).
/// A list added to EhrData later gets its own file automatically.
/// </summary>
public static class EhrDataFiles
{
    /// <summary>File of the counters (QrCounter).</summary>
    public const string CountersFile = "counters.json";

    /// <summary>The list / table properties of EhrData (everything except the counters).</summary>
    private static readonly PropertyInfo[] Collections = typeof(EhrData).GetProperties()
        .Where(property => property.PropertyType != typeof(int))
        .ToArray();

    /// <summary>File name of a property, e.g. DocumentContent -> "document-content.json".</summary>
    public static string FileName(PropertyInfo property)
    {
        System.Text.StringBuilder name = new System.Text.StringBuilder();
        foreach (char c in property.Name)
        {
            if (char.IsUpper(c) && name.Length > 0)
            {
                name.Append('-');
            }
            name.Append(char.ToLowerInvariant(c));
        }
        return name.Append(".json").ToString();
    }

    /// <summary>
    /// Reads the files of a folder into data. A missing file leaves that list empty.
    /// Returns how many files were read.
    /// </summary>
    public static int Read(EhrData data, string folder, JsonSerializerOptions json)
    {
        int read = 0;
        foreach (PropertyInfo property in Collections)
        {
            string path = Path.Combine(folder, FileName(property));
            if (!File.Exists(path))
            {
                continue;
            }
            object? value = JsonSerializer.Deserialize(File.ReadAllText(path), property.PropertyType, json);
            if (value != null)
            {
                property.SetValue(data, value);
            }
            read++;
        }

        string countersPath = Path.Combine(folder, CountersFile);
        if (File.Exists(countersPath))
        {
            Counters? counters = JsonSerializer.Deserialize<Counters>(File.ReadAllText(countersPath), json);
            data.QrCounter = counters?.QrCounter ?? 0;
            read++;
        }
        return read;
    }

    /// <summary>Every file of data with its JSON text: file name -> content.</summary>
    public static Dictionary<string, string> Serialize(EhrData data, JsonSerializerOptions json)
    {
        Dictionary<string, string> files = new Dictionary<string, string>();
        foreach (PropertyInfo property in Collections)
        {
            files[FileName(property)] = JsonSerializer.Serialize(property.GetValue(data), property.PropertyType, json);
        }
        Counters counters = new Counters();
        counters.QrCounter = data.QrCounter;
        files[CountersFile] = JsonSerializer.Serialize(counters, json);
        return files;
    }

    /// <summary>Content of counters.json.</summary>
    private class Counters
    {
        public int QrCounter { get; set; }
    }
}
