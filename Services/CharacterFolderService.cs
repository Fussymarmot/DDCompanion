// Manages user-defined character folders (like Telegram chat folders).
// Folders and character assignments live in a single folders.json file next to
// the .ddc files, so the .ddc format itself is never changed and files stay shareable.
// A character that has no assignment belongs to the built-in "All" tab (main folder).

using System.Text.Json;

namespace DDCompanion.Services;

public sealed class CharacterFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
}

public sealed class CharacterFolderService
{
    private const string FileName = "folders.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly CharacterStorageService storage;
    private readonly SemaphoreSlim gate = new(1, 1);
    private FolderData? data;

    public CharacterFolderService(CharacterStorageService storage)
    {
        this.storage = storage;
    }

    private string FilePath => Path.Combine(storage.GetCharactersDirectory(), FileName);

    private async Task<FolderData> EnsureLoadedAsync()
    {
        if (data is not null)
            return data;

        try
        {
            if (File.Exists(FilePath))
            {
                await using var stream = File.OpenRead(FilePath);
                data = await JsonSerializer.DeserializeAsync<FolderData>(stream, JsonOptions);
            }
        }
        catch (JsonException) { }
        catch (IOException) { }

        data ??= new FolderData();
        data.Folders ??= [];
        data.Assignments ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        data.Assignments = new Dictionary<string, string>(data.Assignments, StringComparer.OrdinalIgnoreCase);
        return data;
    }

    private async Task SaveAsync()
    {
        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, data, JsonOptions);
    }

    public async Task<IReadOnlyList<CharacterFolder>> GetFoldersAsync()
    {
        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();
            return d.Folders.ToList();
        }
        finally { gate.Release(); }
    }

    // Returns fileName -> folderId for characters that were moved into a folder.
    public async Task<IReadOnlyDictionary<string, string>> GetAssignmentsAsync()
    {
        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();
            return new Dictionary<string, string>(d.Assignments, StringComparer.OrdinalIgnoreCase);
        }
        finally { gate.Release(); }
    }

    public async Task<CharacterFolder?> CreateFolderAsync(string name)
    {
        name = name.Trim();
        if (name.Length == 0)
            return null;

        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();
            var folder = new CharacterFolder { Name = name };
            d.Folders.Add(folder);
            await SaveAsync();
            return folder;
        }
        finally { gate.Release(); }
    }

    public async Task RenameFolderAsync(string folderId, string name)
    {
        name = name.Trim();
        if (name.Length == 0)
            return;

        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();
            var folder = d.Folders.FirstOrDefault(f => f.Id == folderId);
            if (folder is null) return;

            folder.Name = name;
            await SaveAsync();
        }
        finally { gate.Release(); }
    }

    // Deleting a folder never deletes characters: they return to the main folder.
    public async Task DeleteFolderAsync(string folderId)
    {
        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();
            d.Folders.RemoveAll(f => f.Id == folderId);

            foreach (var key in d.Assignments.Where(p => p.Value == folderId).Select(p => p.Key).ToList())
                d.Assignments.Remove(key);

            await SaveAsync();
        }
        finally { gate.Release(); }
    }

    // folderId == null moves the character back to the main folder.
    public async Task MoveCharacterAsync(string fileName, string? folderId)
    {
        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();

            if (folderId is null || d.Folders.All(f => f.Id != folderId))
                d.Assignments.Remove(fileName);
            else
                d.Assignments[fileName] = folderId;

            await SaveAsync();
        }
        finally { gate.Release(); }
    }

    // Called when a character file is deleted so folders.json does not keep stale entries.
    public async Task ForgetCharacterAsync(string fileName)
    {
        await gate.WaitAsync();
        try
        {
            var d = await EnsureLoadedAsync();
            if (d.Assignments.Remove(fileName))
                await SaveAsync();
        }
        finally { gate.Release(); }
    }

    private sealed class FolderData
    {
        public List<CharacterFolder> Folders { get; set; } = [];
        public Dictionary<string, string> Assignments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
