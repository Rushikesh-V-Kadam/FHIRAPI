using System.Text.Json;
using System.Text.Json.Serialization;
using MockEhr.Api.Models;

namespace MockEhr.Api.Storage;

/// <summary>
/// Keeps the EHR data in memory and in a JSON file. Registered as a singleton.
///
/// How to use it in a controller (simple and thread-safe):
///   lock (_store.Sync)
///   {
///       ... read or change _store.Data ...
///       _store.Save();          // only after a change
///   }
/// </summary>
public class EhrStore
{
    /// <summary>Only 200 write-backs are kept.</summary>
    private const int MaxWriteBacks = 200;

    private readonly string _filePath;
    private readonly ILogger<EhrStore> _logger;

    /// <summary>JSON settings of the data file: camelCase, indented (easy to read and edit by hand).</summary>
    public static readonly JsonSerializerOptions FileJson = CreateFileJson();

    /// <summary>Lock object: every read / change of Data must be inside lock (Sync).</summary>
    public object Sync { get; } = new object();

    /// <summary>All EHR data.</summary>
    public EhrData Data { get; private set; } = new EhrData();

    /// <summary>Loads the JSON file, or creates it from SeedData when it does not exist yet.</summary>
    public EhrStore(IConfiguration configuration, IWebHostEnvironment environment, ILogger<EhrStore> logger)
    {
        _logger = logger;
        string configured = configuration["MockEhr:DataFile"] ?? "Data/ehr-data.json";
        _filePath = Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
        Load();
    }

    /// <summary>Full path of the data file.</summary>
    public string GetFilePath()
    {
        return _filePath;
    }

    /// <summary>Writes Data to the JSON file (call inside lock (Sync)).</summary>
    public void Save()
    {
        string? folder = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(folder))
        {
            Directory.CreateDirectory(folder);
        }
        // write to a temp file first so a crash never leaves half a file
        string tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(Data, FileJson));
        ReplaceFile(tempPath, _filePath);
    }

    /// <summary>
    /// Puts the temp file in place of the data file. Retries a few times when Windows refuses (file briefly locked by
    /// antivirus / sync, or read-only), and as a last resort copies over the file instead of moving.
    /// </summary>
    private static void ReplaceFile(string tempPath, string targetPath)
    {
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                if (File.Exists(targetPath))
                {
                    File.SetAttributes(targetPath, FileAttributes.Normal);   // clears read-only
                }
                File.Move(tempPath, targetPath, true);
                return;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
            {
                if (attempt == 5)
                {
                    File.Copy(tempPath, targetPath, true);
                    File.Delete(tempPath);
                    return;
                }
                Thread.Sleep(100 * attempt);
            }
        }
    }

    /// <summary>Deletes all data and loads the seed data again (POST /api/admin/reset).</summary>
    public void Reset()
    {
        lock (Sync)
        {
            Data = new EhrData();
            SeedData.Load(Data);
            Save();
        }
    }

    /// <summary>Records a gateway write-back (call inside lock (Sync)).</summary>
    public void LogWriteBack(string kind, string id, object body)
    {
        WriteBack entry = new WriteBack();
        entry.At = DateTimeOffset.UtcNow;
        entry.Kind = kind;
        entry.Id = id;
        entry.Body = JsonSerializer.SerializeToElement(body, FileJson);
        Data.WriteBacks.Add(entry);
        while (Data.WriteBacks.Count > MaxWriteBacks)
        {
            Data.WriteBacks.RemoveAt(0);
        }
    }

    /// <summary>A new id with a prefix, e.g. "pat-4f1c9a2b".</summary>
    public static string NewId(string prefix)
    {
        return prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    /// <summary>Reads the file, or seeds and writes it.</summary>
    private void Load()
    {
        lock (Sync)
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                EhrData? data = JsonSerializer.Deserialize<EhrData>(json, FileJson);
                Data = data ?? new EhrData();
                _logger.LogInformation("EHR data loaded from {Path}", _filePath);
                return;
            }

            Data = new EhrData();
            SeedData.Load(Data);
            Save();
            _logger.LogInformation("EHR data file created with seed data: {Path}", _filePath);
        }
    }

    /// <summary>Builds the file JSON settings.</summary>
    private static JsonSerializerOptions CreateFileJson()
    {
        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.WriteIndented = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        return options;
    }
}
