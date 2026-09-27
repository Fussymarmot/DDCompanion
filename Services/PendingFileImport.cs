namespace DDCompanion.Services;

/// <summary>
/// Signals that Android received a file while the Blazor UI may already be running.
/// The actual file is kept in MAUI preferences/cache so cold starts can process it too.
/// </summary>
public static class PendingFileImport
{
    public static event Action? Requested;

    public static void NotifyRequested() => Requested?.Invoke();
}
