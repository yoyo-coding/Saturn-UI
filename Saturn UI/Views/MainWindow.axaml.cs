using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SaturnUI.Services;

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
    /// 设置启动画面图标(由 App.axaml.cs 在初始化时调用)
    /// </summary>
    public void SetSplashIcon(string theme)
    {
        var isLight = ThemeDefinitions.IsLightTheme(theme);
        var iconName = isLight
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
    /// 淡出并移除启动遮罩
    /// </summary>
    public void DismissSplash()
    {
        var overlay = this.FindControl<Border>("SplashOverlay");
        if (overlay == null) return;

        // 使用 Transitions 实现平滑淡出
        overlay.Transitions = new Avalonia.Animation.Transitions
        {
            new Avalonia.Animation.DoubleTransition
            {
                Property = Border.OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(400)
            }
        };
        overlay.Opacity = 0;

        // 动画结束后隐藏遮罩
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
