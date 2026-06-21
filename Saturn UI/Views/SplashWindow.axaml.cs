using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SaturnUI.Services;

namespace SaturnUI.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    public void ApplyTheme(string themeKey)
    {
        var isLight = themeKey == "Daylight";

        // 背景
        RootGrid.Background = isLight
            ? new SolidColorBrush(Color.Parse("#F0F2F5"))
            : new SolidColorBrush(Color.Parse("#0B0D12"));

        // 文字颜色
        SplashText.Foreground = isLight
            ? new SolidColorBrush(Color.Parse("#1A1D23"))
            : new SolidColorBrush(Color.Parse("#E8ECF1"));

        // 图标
        try
        {
            var iconName = isLight
                ? "avares://SaturnUI/Themes/icon/icon_light.png"
                : "avares://SaturnUI/Themes/icon/icon_dark.png";
            var stream = AssetLoader.Open(new Uri(iconName));
            SplashImage.Source = new Bitmap(stream);
        }
        catch { /* ignore icon load errors */ }
    }
}
