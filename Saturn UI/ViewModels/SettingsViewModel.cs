using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _httpBaseUrl = "http://127.0.0.1:8000";

    [ObservableProperty]
    private string _grpcAddress = "http://127.0.0.1:50051";

    [ObservableProperty]
    private string _protocol = "HTTP";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private bool _performanceMode;

    [ObservableProperty]
    private string _theme = "DeepSpace";

    public IReadOnlyList<string> AvailableThemes => ThemeDefinitions.ThemeKeys;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        Load();
    }

    private void Load()
    {
        var s = _settingsService.Settings;
        HttpBaseUrl = s.HttpBaseUrl;
        GrpcAddress = s.GrpcAddress;
        Protocol = s.Protocol;
        FontSize = s.FontSize;
        PerformanceMode = s.PerformanceMode;
        Theme = s.Theme;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        _settingsService.Update(s =>
        {
            s.HttpBaseUrl = HttpBaseUrl;
            s.GrpcAddress = GrpcAddress;
            s.Protocol = Protocol;
            s.FontSize = FontSize;
            s.PerformanceMode = PerformanceMode;
            s.Theme = Theme;
        });
    }

    [RelayCommand]
    private void ResetSettings()
    {
        HttpBaseUrl = "http://127.0.0.1:8000";
        GrpcAddress = "http://127.0.0.1:50051";
        Protocol = "HTTP";
        FontSize = 14;
        PerformanceMode = false;
        Theme = "DeepSpace";
        SaveSettings();
    }
}
