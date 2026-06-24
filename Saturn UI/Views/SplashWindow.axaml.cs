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

        // 背景(M3 主题令牌)
        RootGrid.Background = isLight
            ? new SolidColorBrush(Color.Parse("#F9FAFD"))
            : new SolidColorBrush(Color.Parse("#0F1115"));

        // 文字颜色(M3 onBackground)
        SplashText.Foreground = isLight
            ? new SolidColorBrush(Color.Parse("#1A1B21"))
            : new SolidColorBrush(Color.Parse("#E3E2E6"));

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
