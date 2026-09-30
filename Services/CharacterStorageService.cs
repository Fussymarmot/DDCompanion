// Loads, saves, imports and deletes characters stored as .ddc JSON files.
// On Windows the Characters folder lives in the user's Documents
// (Documents\DDCompanion\Characters) so anyone can find, back up and copy the
// files. On other platforms it stays in the app's private data folder.

using System.Text;
using System.Text.Json;
using DDCompanion.Models;
using Microsoft.Maui.Storage;

namespace DDCompanion.Services;

public sealed class CharacterStorageService
{
    private const string CharactersFolderName = "Characters";
    private const string AppFolderName = "DDCompanion";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    // Legacy location used by all platforms before the Windows folder moved to Documents.
    private static string LegacyCharactersDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, CharactersFolderName);

    private readonly string charactersDirectory;

    private string CharactersDirectory => charactersDirectory;

    public CharacterStorageService()
    {
        charactersDirectory = ResolveCharactersDirectory();
        Directory.CreateDirectory(charactersDirectory);

#if WINDOWS
        MigrateLegacyCharacters();
#endif
    }

    private static string ResolveCharactersDirectory()
    {
#if WINDOWS
        // GetFolderPath respects a redirected Documents folder (e.g. OneDrive).
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        if (!string.IsNullOrWhiteSpace(documents))
        {
            try
            {
                var path = Path.Combine(documents, AppFolderName, CharactersFolderName);
                Directory.CreateDirectory(path);
                return path;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Documents is not writable - fall back to the app data folder below.
            }
        }
#endif
        return LegacyCharactersDirectory;
    }

#if WINDOWS
    // Moves characters saved by earlier versions (inside the hidden AppData
    // folder) into Documents. Existing files in the new folder are never overwritten.
    private void MigrateLegacyCharacters()
    {
        var legacy = LegacyCharactersDirectory;

        if (!Directory.Exists(legacy) ||
            string.Equals(Path.GetFullPath(legacy), Path.GetFullPath(charactersDirectory), StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var file in Directory.EnumerateFiles(legacy, "*.ddc"))
        {
            try
            {
                var target = Path.Combine(charactersDirectory, Path.GetFileName(file));

                if (File.Exists(target))
                    continue;

                File.Move(file, target);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
#endif

    public async Task<IReadOnlyList<Character>> LoadAllAsync()
    {
        var result = new List<Character>();

        foreach (var file in Directory.EnumerateFiles(CharactersDirectory, "*.ddc"))
        {
            try
            {
                var character = await LoadAsync(file);
                if (character is not null)
                {
                    character.StorageFileName = Path.GetFileName(file);
                    result.Add(character);
                }
            }
            catch (JsonException) { }
            catch (IOException) { }
        }

        return result;
    }

    public async Task<Character?> LoadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<DdcCharacterFile>(stream, JsonOptions);

        if (data is null || !string.Equals(data.Format, "DDC", StringComparison.OrdinalIgnoreCase))
            return null;

        // Legacy DDC files (version 1) did not persist ability modifiers.
        // Migrate them once; newer files are never recalculated on load.
        if (data.Version < 2)
        {
            data.Character.Stats.RecalculateModifiers();
            await SaveToPathAsync(data.Character, path);
        }

        data.Character.StorageFileName = Path.GetFileName(path);
        return data.Character;
    }

    public async Task<string> SaveAsync(Character character)
    {
        Directory.CreateDirectory(CharactersDirectory);

        var baseName = MakeSafeFileName(character.Name);
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "character";

        var path = Path.Combine(CharactersDirectory, $"{baseName}.ddc");

        if (File.Exists(path))
            throw new CharacterAlreadyExistsException($"Персонаж с именем «{character.Name}» уже существует.");

        await SaveToPathAsync(character, path);
        character.StorageFileName = Path.GetFileName(path);

        return path;
    }

    public async Task SaveToPathAsync(Character character, string path)
    {
        var data = new DdcCharacterFile { Character = character };

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
    }

    public async Task<Character?> ImportAsync(Stream input)
    {
        var data = await JsonSerializer.DeserializeAsync<DdcCharacterFile>(input, JsonOptions);

        if (data is null || !string.Equals(data.Format, "DDC", StringComparison.OrdinalIgnoreCase))
            return null;

        await SaveAsync(data.Character);
        return data.Character;
    }



    public string GetCharactersDirectory() => CharactersDirectory;

    public string GetCharacterPath(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        return Path.Combine(CharactersDirectory, safeName);
    }

    public async Task<bool> DeleteAsync(string fileName)
    {
        var path = GetCharacterPath(fileName);
        if (!File.Exists(path)) return false;
        await Task.Run(() => File.Delete(path));

        return true;
    }

    public sealed class CharacterAlreadyExistsException : Exception
    {
        public CharacterAlreadyExistsException(string message) : base(message) { }
    }

    private static string MakeSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder();

        foreach (var c in value.Trim())
            builder.Append(invalid.Contains(c) ? '_' : c);

        return builder.ToString().Trim();
    }
}
