using System.Text;
using System.Text.Json;
using DDCompanion.Models;
using Microsoft.Maui.Storage;

namespace DDCompanion.Services;

public sealed class CharacterStorageService
{
    private const string CharactersFolderName = "Characters";
    private static readonly SemaphoreSlim PendingImportLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private string CharactersDirectory => Path.Combine(FileSystem.AppDataDirectory, CharactersFolderName);

    public CharacterStorageService() => Directory.CreateDirectory(CharactersDirectory);

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

        // Старые DDC-файлы не содержали сохранённых модификаторов.
        // Мигрируем их один раз; новые файлы больше ничего при загрузке не пересчитывают.
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



    public async Task<bool> ImportPendingAndroidFileAsync()
    {
#if ANDROID
        await PendingImportLock.WaitAsync();
        try
        {
            var pendingPath = Preferences.Default.Get("pending_ddc_path", string.Empty);
            if (!string.IsNullOrWhiteSpace(pendingPath))
            {
                try
                {
                    if (!File.Exists(pendingPath))
                        return false;

                    await using var fileStream = File.OpenRead(pendingPath);
                    var character = await ImportAsync(fileStream);
                    if (character is null)
                        return false;

                    Preferences.Default.Remove("pending_ddc_path");
                    Preferences.Default.Remove("pending_ddc_uri");
                    TryDeleteFile(pendingPath);
                    return true;
                }
                catch
                {
                    Preferences.Default.Remove("pending_ddc_path");
                    TryDeleteFile(pendingPath);
                    return false;
                }
            }

            var uriString = Preferences.Default.Get("pending_ddc_uri", string.Empty);
            if (string.IsNullOrWhiteSpace(uriString))
                return false;

            try
            {
                var uri = Android.Net.Uri.Parse(uriString);
                if (uri is null)
                    return false;

                var resolver = Android.App.Application.Context.ContentResolver;
                await using var stream = resolver.OpenInputStream(uri);
                if (stream is null)
                    return false;

                var character = await ImportAsync(stream);
                if (character is null)
                    return false;

                Preferences.Default.Remove("pending_ddc_uri");
                return true;
            }
            catch
            {
                Preferences.Default.Remove("pending_ddc_uri");
                return false;
            }
        }
        finally
        {
            PendingImportLock.Release();
        }
#else
        return false;
#endif
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Cache cleanup is best-effort.
        }
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
