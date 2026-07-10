using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using SaturnUI.Services;
using SaturnUI.Services.Coding;
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
            themeService.Initialize();

            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };

            mainWindow.SetSplashIcon(settings.Settings.UseLightTheme);
            ApplyWindowIcon(mainWindow, settings.Settings.UseLightTheme);

            themeService.ThemeChanged += (_, useLightTheme) =>
            {
                ApplyWindowIcon(mainWindow, useLightTheme);
            };

            desktop.MainWindow = mainWindow;
            mainWindow.Show();

            _ = DismissSplashAsync(mainWindow);
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

    private async Task DismissSplashAsync(MainWindow mainWindow)
    {
        await Task.Delay(1000);
        mainWindow.DismissSplash();
    }

    private static void ApplyWindowIcon(Window window, bool useLightTheme)
    {
        try
        {
            var iconName = useLightTheme
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
        services.AddSingleton<CodeLanguageService>();
        services.AddSingleton<CodeFileService>();
        services.AddSingleton<CodeAssistantService>();
        services.AddSingleton<CodeHighlightingService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ChatViewModel>();
        services.AddSingleton<SessionListViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<CodingWorkspaceViewModel>();
    }
}

