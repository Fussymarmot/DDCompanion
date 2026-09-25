using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.Storage;

namespace DDCompanion;

[Activity(
    Theme = "@style/DDCompanionTheme",
    MainLauncher = true,
    Icon = "@mipmap/ic_launcher",
    ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize
                           | ConfigChanges.Orientation
                           | ConfigChanges.UiMode
                           | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize
                           | ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTask)]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "content" },
    DataMimeTypes = new[] { "application/octet-stream", "*/*" })]
[IntentFilter(
    new[] { Intent.ActionView },
    Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
    DataSchemes = new[] { "file" },
    DataMimeTypes = new[] { "application/octet-stream", "*/*" })]
[IntentFilter(
    new[] { Intent.ActionSend },
    Categories = new[] { Intent.CategoryDefault },
    DataMimeTypes = new[] { "application/octet-stream", "*/*" })]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        Window.SetStatusBarColor(global::Android.Graphics.Color.Black);
        Window.SetNavigationBarColor(global::Android.Graphics.Color.Black);

        HandleIncomingIntent(Intent);

        WindowCompat.SetDecorFitsSystemWindows(Window, false);
        ViewCompat.SetOnApplyWindowInsetsListener(Window.DecorView, new InsetsListener());
        ViewCompat.RequestApplyInsets(Window.DecorView);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);

        if (intent is not null)
            HandleIncomingIntent(intent);
    }

    private static void HandleIncomingIntent(Intent? intent)
    {
        if (intent is null)
            return;

        var uri = intent.Data;

        if (uri is null && intent.ClipData is not null && intent.ClipData.ItemCount > 0)
            uri = intent.ClipData.GetItemAt(0)?.Uri;

        if (uri is null && intent.Action == Intent.ActionSend)
            uri = intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;

        if (uri is not null)
            SaveIncomingUri(uri);
    }

    private static void SaveIncomingUri(Android.Net.Uri uri)
    {
        try
        {
            var pendingPath = Path.Combine(
                FileSystem.CacheDirectory,
                $"pending-{Guid.NewGuid():N}.ddc");

            if (string.Equals(uri.Scheme, "file", StringComparison.OrdinalIgnoreCase)
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
                    throw new IOException("Не удалось открыть файл через ContentResolver.");

                using var output = File.Create(pendingPath);
                input.CopyTo(output);
            }

            Preferences.Default.Set("pending_ddc_path", pendingPath);
            Preferences.Default.Remove("pending_ddc_uri");

        }
        catch
        {
            // A content URI may only be accessible through a temporary Android grant.
            // Fall back to storing the URI and opening it from inside the app.
            var uriString = uri.ToString();
            Preferences.Default.Set("pending_ddc_uri", uriString);
            Preferences.Default.Remove("pending_ddc_path");
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
