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

        Preferences.Default.Set("pending_ddc_uri", uri.ToString());
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
