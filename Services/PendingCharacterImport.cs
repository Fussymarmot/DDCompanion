// Bridge between platform code (e.g. an Android Activity that received an
// intent to open a character file) and the Blazor UI.
//
// Previously this was done via Preferences and checked only once in
// Home.OnInitializedAsync, which caused two problems:
//  - on a "warm" start (app already running, user opens another file)
//    nothing happened, because Home.OnInitializedAsync is not called again;
//  - a successful import gave no visible feedback — the Home list was
//    refreshed, but the file itself was never "opened".
//
// Now platform code just calls <see cref="Set"/>, while Blazor subscribes to
// <see cref="Requested"/> once in MainLayout (which lives for the whole app
// lifetime) and navigates to the imported character's page itself.
namespace DDCompanion.Services;
public static class PendingCharacterImport
{
    private static string? _pendingUri;

    public static event Action? Requested;

    public static void Set(string uri)
    {
        _pendingUri = uri;
        Requested?.Invoke();
    }

    public static string? TakePendingUri()
    {
        var uri = _pendingUri;
        _pendingUri = null;
        return uri;
    }
}
