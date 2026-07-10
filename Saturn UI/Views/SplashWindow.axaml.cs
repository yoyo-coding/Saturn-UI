using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace SaturnUI.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void ApplyTheme(bool useLightTheme)
    {
        RootGrid.Background = useLightTheme
            ? new SolidColorBrush(Color.Parse("#FFFBFF"))
            : new SolidColorBrush(Color.Parse("#141218"));

        SplashText.Foreground = useLightTheme
            ? new SolidColorBrush(Color.Parse("#1D1B20"))
            : new SolidColorBrush(Color.Parse("#E6E0E9"));

        try
        {
            var iconName = useLightTheme
                ? "avares://SaturnUI/Themes/icon/icon_light.png"
                : "avares://SaturnUI/Themes/icon/icon_dark.png";
            var stream = AssetLoader.Open(new Uri(iconName));
            SplashImage.Source = new Bitmap(stream);
        }
        catch { /* ignore icon load errors */ }
    }
}
