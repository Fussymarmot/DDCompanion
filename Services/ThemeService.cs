using Microsoft.Maui.Storage;

namespace DDCompanion.Services;

public sealed class ThemeService
{
    private const string ThemeLightKey = "theme_light";

    public bool IsLightTheme => Preferences.Default.Get(ThemeLightKey, false);

    public event Action? ThemeChanged;

    public void SetLightTheme(bool enabled)
    {
        Preferences.Default.Set(ThemeLightKey, enabled);
        ThemeChanged?.Invoke();
    }
}
