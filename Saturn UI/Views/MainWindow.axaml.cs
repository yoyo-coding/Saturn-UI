using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace SaturnUI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Sets the splash icon according to the current light/dark appearance.
    /// </summary>
    public void SetSplashIcon(bool useLightTheme)
    {
        var iconName = useLightTheme
            ? "avares://SaturnUI/Themes/icon/icon_light.png"
            : "avares://SaturnUI/Themes/icon/icon_dark.png";

        try
        {
            var stream = AssetLoader.Open(new Uri(iconName));
            var img = this.FindControl<Image>("SplashImage");
            if (img != null)
                img.Source = new Bitmap(stream);
        }
        catch { /* ignore icon load errors */ }
    }

    /// <summary>
    /// Fades out and hides the splash overlay.
    /// </summary>
    public void DismissSplash()
    {
        var overlay = this.FindControl<Border>("SplashOverlay");
        if (overlay == null) return;

        overlay.Transitions = new Avalonia.Animation.Transitions
        {
            new Avalonia.Animation.DoubleTransition
            {
                Property = Border.OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(400)
            }
        };
        overlay.Opacity = 0;

        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            overlay.IsVisible = false;
        };
        timer.Start();
    }
}
