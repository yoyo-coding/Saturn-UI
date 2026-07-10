using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

public sealed record ColorPaletteOption(string Name, string Color)
{
    public IBrush Brush => new SolidColorBrush(Avalonia.Media.Color.Parse(Color));
}

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _appearanceSaveTimer;
    private bool _isLoading;
    private bool _isNormalizingAccentColor;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _httpBaseUrl = AppConstants.DefaultHttpBaseUrl;

    [ObservableProperty]
    private string _grpcAddress = AppConstants.DefaultGrpcAddress;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _protocol = AppConstants.ProtocolHttp;

    [ObservableProperty]
    private double _fontSize = AppConstants.DefaultFontSize;

    [ObservableProperty]
    private bool _performanceMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccentPreviewBrush))]
    [NotifyPropertyChangedFor(nameof(NormalizedAccentColor))]
    private string _accentColor = AppConstants.DefaultAccentColor;

    [ObservableProperty]
    private bool _useLightTheme = AppConstants.DefaultUseLightTheme;

    [ObservableProperty]
    private ColorPaletteOption? _selectedAccentOption;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProviderConfigButtonText))]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _provider = AppConstants.ProviderLocal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _openAiApiKey = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _openAiModel = AppConstants.DefaultOpenAiModel;

    [ObservableProperty]
    private string _openAiBaseUrl = AppConstants.DefaultOpenAiBaseUrl;

    [ObservableProperty]
    private double _openAiTemperature = AppConstants.DefaultTemperature;

    [ObservableProperty]
    private int _openAiMaxTokens = AppConstants.DefaultMaxTokens;

    public IReadOnlyList<ColorPaletteOption> AccentColorOptions { get; } = new[]
    {
        new ColorPaletteOption("暮紫", "#6750A4"),
        new ColorPaletteOption("星蓝", "#1A73E8"),
        new ColorPaletteOption("湖青", "#00897B"),
        new ColorPaletteOption("森林", "#2E7D32"),
        new ColorPaletteOption("日落", "#E8710A"),
        new ColorPaletteOption("莓红", "#C2185B"),
        new ColorPaletteOption("赤陶", "#B3261E"),
        new ColorPaletteOption("石墨", "#546E7A")
    };

    public IReadOnlyList<string> AvailableProviders => new[] { AppConstants.ProviderLocal, AppConstants.ProviderOnline };

    public IBrush AccentPreviewBrush => new SolidColorBrush(DynamicColorPalette.ParseSeed(AccentColor));

    public string NormalizedAccentColor => DynamicColorPalette.NormalizeHexColor(AccentColor);

    public string ProviderConfigButtonText => Provider == AppConstants.ProviderLocal
        ? "配置本地后端"
        : "配置 OpenAI 兼容服务";

    public string CurrentProviderStatus
    {
        get
        {
            if (Provider == AppConstants.ProviderLocal)
            {
                return $"本地后端 | 协议：{Protocol} | HTTP：{HttpBaseUrl}";
            }

            var keyStatus = string.IsNullOrWhiteSpace(OpenAiApiKey) ? "未设置" : "已配置";
            return $"在线服务 | 模型：{OpenAiModel} | API Key：{keyStatus}";
        }
    }

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _appearanceSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _appearanceSaveTimer.Tick += (_, _) =>
        {
            _appearanceSaveTimer.Stop();
            CommitAppearanceSettings();
        };

        Load();
    }

    private void Load()
    {
        _isLoading = true;
        try
        {
            var s = _settingsService.Settings;
            HttpBaseUrl = s.HttpBaseUrl;
            GrpcAddress = s.GrpcAddress;
            Protocol = s.Protocol;
            FontSize = s.FontSize;
            PerformanceMode = s.PerformanceMode;
            AccentColor = s.AccentColor;
            UseLightTheme = s.UseLightTheme;
            Provider = s.Provider;

            OpenAiApiKey = s.OpenAiApiKey;
            OpenAiModel = s.OpenAiModel;
            OpenAiBaseUrl = s.OpenAiBaseUrl;
            OpenAiTemperature = s.OpenAiTemperature;
            OpenAiMaxTokens = s.OpenAiMaxTokens;
        }
        finally
        {
            _isLoading = false;
        }

        OnPropertyChanged(nameof(ProviderConfigButtonText));
        OnPropertyChanged(nameof(CurrentProviderStatus));
    }


    partial void OnAccentColorChanged(string value)
    {
        if (_isLoading || _isNormalizingAccentColor)
            return;

        OnPropertyChanged(nameof(AccentPreviewBrush));
        OnPropertyChanged(nameof(NormalizedAccentColor));

        if (DynamicColorPalette.TryNormalizeHexColor(value, out _))
            ScheduleAppearanceCommit();
    }

    partial void OnUseLightThemeChanged(bool value)
    {
        if (!_isLoading)
            ScheduleAppearanceCommit();
    }

    private void ScheduleAppearanceCommit()
    {
        _appearanceSaveTimer.Stop();
        _appearanceSaveTimer.Start();
    }

    private void CommitAppearanceSettings()
    {
        if (!DynamicColorPalette.TryNormalizeHexColor(AccentColor, out var normalizedAccent))
            return;

        if (AccentColor != normalizedAccent)
        {
            _isNormalizingAccentColor = true;
            try
            {
                AccentColor = normalizedAccent;
            }
            finally
            {
                _isNormalizingAccentColor = false;
            }
        }

        _settingsService.Update(s =>
        {
            s.AccentColor = normalizedAccent;
            s.UseLightTheme = UseLightTheme;
        });

        OnPropertyChanged(nameof(AccentPreviewBrush));
        OnPropertyChanged(nameof(NormalizedAccentColor));
    }

    [RelayCommand]
    public void SaveSettings()
    {
        var normalizedAccent = DynamicColorPalette.NormalizeHexColor(AccentColor);
        if (AccentColor != normalizedAccent)
            AccentColor = normalizedAccent;

        _settingsService.Update(s =>
        {
            s.HttpBaseUrl = HttpBaseUrl;
            s.GrpcAddress = GrpcAddress;
            s.Protocol = Protocol;
            s.FontSize = FontSize;
            s.PerformanceMode = PerformanceMode;
            s.AccentColor = normalizedAccent;
            s.UseLightTheme = UseLightTheme;
            s.Provider = Provider;

            s.OpenAiApiKey = OpenAiApiKey;
            s.OpenAiModel = OpenAiModel;
            s.OpenAiBaseUrl = OpenAiBaseUrl;
            s.OpenAiTemperature = OpenAiTemperature;
            s.OpenAiMaxTokens = OpenAiMaxTokens;
        });

        OnPropertyChanged(nameof(AccentPreviewBrush));
        OnPropertyChanged(nameof(NormalizedAccentColor));
        OnPropertyChanged(nameof(ProviderConfigButtonText));
        OnPropertyChanged(nameof(CurrentProviderStatus));
    }

    [RelayCommand]
    private void SelectAccentColor(string color)
    {
        AccentColor = DynamicColorPalette.NormalizeHexColor(color);
        SaveSettings();
    }

    partial void OnSelectedAccentOptionChanged(ColorPaletteOption? value)
    {
        if (value is null)
            return;

        var normalized = DynamicColorPalette.NormalizeHexColor(value.Color);
        if (AccentColor != normalized)
            AccentColor = normalized;

        SaveSettings();
    }

    [RelayCommand]
    private void ResetSettings()
    {
        HttpBaseUrl = AppConstants.DefaultHttpBaseUrl;
        GrpcAddress = AppConstants.DefaultGrpcAddress;
        Protocol = AppConstants.ProtocolHttp;
        FontSize = AppConstants.DefaultFontSize;
        PerformanceMode = false;
        AccentColor = AppConstants.DefaultAccentColor;
        UseLightTheme = AppConstants.DefaultUseLightTheme;
        Provider = AppConstants.ProviderLocal;

        OpenAiApiKey = string.Empty;
        OpenAiModel = AppConstants.DefaultOpenAiModel;
        OpenAiBaseUrl = AppConstants.DefaultOpenAiBaseUrl;
        OpenAiTemperature = AppConstants.DefaultTemperature;
        OpenAiMaxTokens = AppConstants.DefaultMaxTokens;

        SaveSettings();
    }

    public event EventHandler<string>? OpenProviderConfigRequested;

    [RelayCommand]
    private void OpenProviderConfig()
    {
        OpenProviderConfigRequested?.Invoke(this, Provider);
    }

    public void RefreshStatus()
    {
        OnPropertyChanged(nameof(ProviderConfigButtonText));
        OnPropertyChanged(nameof(CurrentProviderStatus));
    }
}
