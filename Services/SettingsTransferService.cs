using System.Text;
using System.Text.Json;
using DDCompanion.Models;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace DDCompanion.Services;

public sealed class SettingsTransferService
{
    private const string FileFormat = "DDCompanionSettings";
    private const int CurrentVersion = 1;
    private const string Extension = ".ddcs";
    private const string FileName = "DDCompanion-settings.ddcs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<string> CreateExportFileAsync()
    {
        var document = CreateSettingsDocument();
        var path = Path.Combine(FileSystem.CacheDirectory, FileName);
        var json = JsonSerializer.Serialize(document, JsonOptions);
        await File.WriteAllTextAsync(path, json);
        return path;
    }

    public async Task<string?> ExportAsync()
    {
        var document = CreateSettingsDocument();
        var json = JsonSerializer.Serialize(document, JsonOptions);

#if ANDROID
        // Use Android's system "Save as" picker. The user chooses the folder
        // and filename instead of the app silently writing to Downloads.
        // application/octet-stream prevents Android from replacing .ddcs with .json.
        return await MainActivity.SaveFileAsync(
            Encoding.UTF8.GetBytes(json),
            FileName,
            "application/octet-stream");
#else
        var path = Path.Combine(GetExportDirectory(), FileName);
        await File.WriteAllTextAsync(path, json);
        return path;
#endif
    }

    // Windows: Documents\DDCompanion, next to the Characters folder, so the file is easy to find.
    // Other platforms: the app's private data folder.
    private static string GetExportDirectory()
    {
#if WINDOWS
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (!string.IsNullOrWhiteSpace(documents))
        {
            try
            {
                var path = Path.Combine(documents, "DDCompanion");
                Directory.CreateDirectory(path);
                return path;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
#endif
        return FileSystem.AppDataDirectory;
    }

    public async Task ShareAsync(string title)
    {
        var path = await CreateExportFileAsync();
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = title,
            File = new ShareFile(path, "application/json")
        });
    }

    public async Task ImportAsync(Stream stream)
    {
        var document = await JsonSerializer.DeserializeAsync<SettingsDocument>(stream, JsonOptions)
            ?? throw new InvalidDataException("Файл настроек пуст или повреждён.");

        if (!string.Equals(document.Format, FileFormat, StringComparison.Ordinal) || document.Version < 1)
            throw new InvalidDataException("Это не файл настроек D&D Companion.");

        if (document.Classes is null || document.Races is null)
            throw new InvalidDataException("В файле отсутствуют списки классов или рас.");

        CharacterOptions.ReplaceClasses(document.Classes);
        CharacterOptions.ReplaceRaces(document.Races);
    }

    private static SettingsDocument CreateSettingsDocument()
    {
        return new SettingsDocument
        {
            Format = FileFormat,
            Version = CurrentVersion,
            Classes = CharacterOptions.Classes.ToArray(),
            Races = CharacterOptions.Races.ToArray()
        };
    }


    private sealed class SettingsDocument
    {
        public string Format { get; set; } = FileFormat;
        public int Version { get; set; } = CurrentVersion;
        public string[]? Classes { get; set; }
        public string[]? Races { get; set; }
    }
}
