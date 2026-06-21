using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using SaturnUI.Services;
using SaturnUI.ViewModels;
using SaturnUI.Views;
using Microsoft.Extensions.DependencyInjection;

namespace SaturnUI;

public partial class App : Application
{
    public new static App Current => (App)Application.Current!;

    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        var settings = Services.GetRequiredService<SettingsService>();
        var themeService = Services.GetRequiredService<ThemeService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var splash = new SplashWindow();
            splash.ApplyTheme(settings.Settings.Theme);
            splash.Show();

            _ = ShowMainWindowDelayedAsync(desktop, splash, settings, themeService);
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            themeService.Initialize();
            singleViewPlatform.MainView = new MainView
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task ShowMainWindowDelayedAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash,
        SettingsService settings,
        ThemeService themeService)
    {
        await Task.Delay(1000);

        themeService.Initialize();

        var mainWindow = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };

        ApplyWindowIcon(mainWindow, settings.Settings.Theme);

        themeService.ThemeChanged += (_, theme) =>
        {
            ApplyWindowIcon(mainWindow, theme);
        };

        desktop.MainWindow = mainWindow;
        mainWindow.Show();
        splash.Close();
    }

    private static void ApplyWindowIcon(Window window, string theme)
    {
        try
        {
            var isLight = theme == "Daylight";
            var iconName = isLight
                ? "avares://SaturnUI/Themes/icon/icon_light.png"
                : "avares://SaturnUI/Themes/icon/icon_dark.png";

            var stream = AssetLoader.Open(new Uri(iconName));
            window.Icon = new WindowIcon(stream);
        }
        catch { /* ignore icon load errors */ }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<SettingsService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocalStorageService>();
        services.AddSingleton<ChatService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<SessionListViewModel>();
        services.AddSingleton<SettingsViewModel>();
    }
}
