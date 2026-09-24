namespace DDCompanion.Services;

/// <summary>
/// Мост между платформенным кодом (например, Android-Activity, получившей
/// intent на открытие файла персонажа) и Blazor UI.
///
/// Раньше это делалось через Preferences и проверялось только один раз —
/// в OnInitializedAsync страницы Home. Из-за этого:
///  - при "тёплом" старте (приложение уже открыто, потом пользователь
///    открывает файл ещё раз) ничего не происходило вообще, потому что
///    Home.OnInitializedAsync уже не вызывается повторно;
///  - при успешном импорте не было видимой реакции — обновлялся список на
///    Home, но сам файл никуда не "открывался", поэтому казалось, что
///    ничего не произошло, если такого персонажа ещё не было.
///
/// Теперь платформенный код просто вызывает <see cref="Set"/>, а Blazor
/// подписывается на <see cref="Requested"/> один раз в MainLayout (который
/// живёт всё время работы приложения) и сам переходит на страницу
/// импортированного персонажа.
/// </summary>
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
