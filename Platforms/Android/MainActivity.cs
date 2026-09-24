using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Storage;

namespace DDCompanion;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    Icon = "@mipmap/ic_launcher",
    ConfigurationChanges = ConfigChanges.ScreenSize
                           | ConfigChanges.Orientation
                           | ConfigChanges.UiMode
                           | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize
                           | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTop)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        HandleOpenIntent(Intent);

        WindowCompat.SetDecorFitsSystemWindows(Window, false);
        ViewCompat.SetOnApplyWindowInsetsListener(Window.DecorView, new InsetsListener());
        ViewCompat.RequestApplyInsets(Window.DecorView);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent is not null)
            HandleOpenIntent(intent);
    }

    private static void HandleOpenIntent(Intent? intent)
    {
        var uri = intent?.Data;
        if (uri is null || !string.Equals(intent?.Action, Intent.ActionView, StringComparison.Ordinal))
            return;

        // File managers normally grant a temporary read permission to the Activity.
        // Copy the file while that grant is definitely alive instead of passing the
        // content:// URI through Preferences and trying to open it later.
        try
        {
            var pendingPath = Path.Combine(
                FileSystem.CacheDirectory,
                $"pending-{Guid.NewGuid():N}.ddc");

            if (uri.Scheme?.Equals("file", StringComparison.OrdinalIgnoreCase) == true
                && !string.IsNullOrWhiteSpace(uri.Path))
            {
                using var input = File.OpenRead(uri.Path);
                using var output = File.Create(pendingPath);
                input.CopyTo(output);
            }
            else
            {
                var resolver = Android.App.Application.Context.ContentResolver;
                using var input = resolver.OpenInputStream(uri);
                if (input is null)
                    throw new IOException("Не удалось открыть выбранный файл.");

                using var output = File.Create(pendingPath);
                input.CopyTo(output);
            }

            Preferences.Default.Set("pending_ddc_path", pendingPath);
            Preferences.Default.Remove("pending_ddc_uri");
        }
        catch
        {
            // Keep the URI as a fallback. CharacterStorageService will retry it
            // after the Blazor page has started, without allowing the app to crash.
            Preferences.Default.Set("pending_ddc_uri", uri.ToString());
        }
    }

    private sealed class InsetsListener
        : Java.Lang.Object, global::AndroidX.Core.View.IOnApplyWindowInsetsListener
    {
        public global::AndroidX.Core.View.WindowInsetsCompat? OnApplyWindowInsets(
            global::Android.Views.View? view,
            global::AndroidX.Core.View.WindowInsetsCompat? insets)
        {
            if (view is null || insets is null)
                return insets;

            var systemBars = insets.GetInsets(
                global::AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());

            view.SetPadding(systemBars.Left, systemBars.Top, systemBars.Right, systemBars.Bottom);
            return insets;
        }
    }
}
