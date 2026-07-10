using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
        UpdateMaximizeRestoreGlyph();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this);
        if (!pointer.Properties.IsLeftButtonPressed)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

        BeginMoveDrag(e);
        e.Handled = true;
    }

    private void OnMinimizeClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object? sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

        UpdateMaximizeRestoreGlyph();
    }

    private void UpdateMaximizeRestoreGlyph()
    {
        var glyph = this.FindControl<TextBlock>("MaximizeRestoreGlyph");
        if (glyph == null) return;

        glyph.Text = WindowState == WindowState.Maximized ? "❐" : "□";
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
