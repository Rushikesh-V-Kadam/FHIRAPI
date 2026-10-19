using System.Text.Json;
using System.Text.Json.Serialization;

namespace MockEhr.Api.Storage;

/// <summary>
/// Keeps the EHR data in memory and in JSON files, one file per resource (EhrDataFiles). Registered as a singleton.
///   Data folder (MockEhr:DataFolder, default Data)              live data: patients.json, orders.json, write-backs.json ...
///   Test data folder (MockEhr:TestDataFolder, default Data/TestData)  the data the EHR starts with. Copied to the data
///                                                               folder on the first start and by POST /api/admin/reset.
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

    private readonly string _dataFolder;
    private readonly string _testDataFolder;
    private readonly ILogger<EhrStore> _logger;

    /// <summary>Content last written per file, so Save only rewrites the files that changed.</summary>
    private readonly Dictionary<string, string> _written = new Dictionary<string, string>();

    /// <summary>JSON settings of the data files: camelCase, indented (easy to read and edit by hand).</summary>
    public static readonly JsonSerializerOptions FileJson = CreateFileJson();

    /// <summary>Lock object: every read / change of Data must be inside lock (Sync).</summary>
    public object Sync { get; } = new object();

    /// <summary>All EHR data.</summary>
    public EhrData Data { get; private set; } = new EhrData();

    /// <summary>Loads the data files, or creates them from the test data when they do not exist yet.</summary>
    public EhrStore(IConfiguration configuration, IWebHostEnvironment environment, ILogger<EhrStore> logger)
    {
        _logger = logger;
        _dataFolder = FullPath(environment, configuration["MockEhr:DataFolder"] ?? "Data");
        _testDataFolder = FullPath(environment, configuration["MockEhr:TestDataFolder"] ?? "Data/TestData");
        Load();
    }

    /// <summary>Full path of the data folder.</summary>
    public string GetDataFolder()
    {
        return _dataFolder;
    }

    /// <summary>Writes the changed lists of Data to their files (call inside lock (Sync)).</summary>
    public void Save()
    {
        Directory.CreateDirectory(_dataFolder);
        foreach (KeyValuePair<string, string> file in EhrDataFiles.Serialize(Data, FileJson))
        {
            if (_written.TryGetValue(file.Key, out string? previous) && previous == file.Value)
            {
                continue;
            }
            // write to a temp file first so a crash never leaves half a file
            string path = Path.Combine(_dataFolder, file.Key);
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, file.Value);
            ReplaceFile(tempPath, path);
            _written[file.Key] = file.Value;
        }
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

    /// <summary>Deletes all data and loads the test data again (POST /api/admin/reset).</summary>
    public void Reset()
    {
        lock (Sync)
        {
            Data = new EhrData();
            SeedData.Load(Data, _testDataFolder);
            _written.Clear();
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

    /// <summary>Reads the data files, or copies the test data when there are none yet.</summary>
    private void Load()
    {
        lock (Sync)
        {
            Data = new EhrData();
            if (EhrDataFiles.Read(Data, _dataFolder, FileJson) > 0)
            {
                _logger.LogInformation("EHR data loaded from {Folder}", _dataFolder);
                return;
            }

            SeedData.Load(Data, _testDataFolder);
            Save();
            _logger.LogInformation("EHR data files created from the test data in {TestFolder}: {Folder}", _testDataFolder, _dataFolder);
        }
    }

    /// <summary>A configured folder, relative to the project folder unless it is a full path.</summary>
    private static string FullPath(IWebHostEnvironment environment, string configured)
    {
        return Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured);
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
