using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;

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
                           | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        ViewCompat.SetOnApplyWindowInsetsListener(
            Window.DecorView,
            new InsetsListener()
        );

        ViewCompat.RequestApplyInsets(Window.DecorView);
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
                global::AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars()
            );

            view.SetPadding(
                systemBars.Left,
                systemBars.Top,
                systemBars.Right,
                systemBars.Bottom
            );

            return insets;
        }
    }
}