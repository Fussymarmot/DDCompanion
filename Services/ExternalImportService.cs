using System.Text.Json;
using DDCompanion.Models;
using Microsoft.Maui.Storage;

namespace DDCompanion.Services;

public sealed class ExternalImportService
{
    private const string PendingPathKey = "pending_import_path";
    private const string PendingUriKey = "pending_import_uri";
    private static readonly SemaphoreSlim ImportLock = new(1, 1);

    private readonly CharacterStorageService characterStorage;
    private readonly SettingsTransferService settingsTransfer;

    public ExternalImportService(
        CharacterStorageService characterStorage,
        SettingsTransferService settingsTransfer)
    {
        this.characterStorage = characterStorage;
        this.settingsTransfer = settingsTransfer;
    }

    public async Task<ExternalImportResult?> ImportPendingAsync()
    {
#if ANDROID
        await ImportLock.WaitAsync();
        try
        {
            await using var source = await OpenPendingStreamAsync();
            if (source is null)
                return null;

            await using var memory = new MemoryStream();
            await source.CopyToAsync(memory);
            memory.Position = 0;

            var format = await ReadFormatAsync(memory);
            memory.Position = 0;

            if (string.Equals(format, "DDC", StringComparison.OrdinalIgnoreCase))
            {
                var character = await characterStorage.ImportAsync(memory);
                ClearPending();
                return character is null ? null : ExternalImportResult.ForCharacter(character);
            }

            if (string.Equals(format, "DDCompanionSettings", StringComparison.Ordinal))
            {
                await settingsTransfer.ImportAsync(memory);
                ClearPending();
                return ExternalImportResult.ForSettings();
            }

            ClearPending();
            return null;
        }
        catch
        {
            ClearPending();
            return null;
        }
        finally
        {
            ImportLock.Release();
        }
#else
        return null;
#endif
    }

#if ANDROID
    private static Task<Stream?> OpenPendingStreamAsync()
    {
        var pendingPath = Preferences.Default.Get(PendingPathKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(pendingPath) && File.Exists(pendingPath))
            return Task.FromResult<Stream?>(File.OpenRead(pendingPath));

        var uriString = Preferences.Default.Get(PendingUriKey, string.Empty);
        if (string.IsNullOrWhiteSpace(uriString))
            return Task.FromResult<Stream?>(null);

        var uri = Android.Net.Uri.Parse(uriString);
        if (uri is null)
            return Task.FromResult<Stream?>(null);

        var resolver = Android.App.Application.Context.ContentResolver;
        return Task.FromResult<Stream?>(resolver.OpenInputStream(uri));
    }

    private static async Task<string?> ReadFormatAsync(Stream stream)
    {
        using var document = await JsonDocument.ParseAsync(stream);

        // Files produced by System.Text.Json use "Format", while some older/
        // third-party exports may contain "format". Accept both (and any casing).
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (string.Equals(property.Name, "format", StringComparison.OrdinalIgnoreCase))
                return property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString()
                    : null;
        }

        return null;
    }

    private static void ClearPending()
    {
        var path = Preferences.Default.Get(PendingPathKey, string.Empty);
        Preferences.Default.Remove(PendingPathKey);
        Preferences.Default.Remove(PendingUriKey);

        if (!string.IsNullOrWhiteSpace(path))
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // Best-effort cache cleanup.
            }
        }
    }
#endif
}

public sealed record ExternalImportResult(string Type, Character? Character)
{
    public bool IsCharacter => string.Equals(Type, "character", StringComparison.Ordinal);
    public bool IsSettings => string.Equals(Type, "settings", StringComparison.Ordinal);

    public static ExternalImportResult ForCharacter(Character character) => new("character", character);
    public static ExternalImportResult ForSettings() => new("settings", null);
}
