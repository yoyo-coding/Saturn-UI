using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly SettingsService _settingsService;

    // 本地后端配置
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _httpBaseUrl = "http://127.0.0.1:8000";

    [ObservableProperty]
    private string _grpcAddress = "http://127.0.0.1:50051";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _protocol = "HTTP";

    [ObservableProperty]
    private double _fontSize = 14;

    [ObservableProperty]
    private bool _performanceMode;

    [ObservableProperty]
    private string _theme = "DeepSpace";

    // 提供商选择
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProviderConfigButtonText))]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _provider = "本地";

    // OpenAI 兼容 API 配置
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _openAiApiKey = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentProviderStatus))]
    private string _openAiModel = "gpt-3.5-turbo";

    [ObservableProperty]
    private string _openAiBaseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private double _openAiTemperature = 0.7;

    [ObservableProperty]
    private int _openAiMaxTokens = 2048;

    public IReadOnlyList<string> AvailableThemes => ThemeDefinitions.ThemeKeys;

    public IReadOnlyList<string> AvailableProviders => new[] { "本地", "在线" };

    /// <summary>
    /// 提供商配置按钮文本
    /// </summary>
    public string ProviderConfigButtonText => Provider == "本地" ? "配置本地后端" : "配置 OpenAI 兼容服务";

    /// <summary>
    /// 当前提供商状态显示
    /// </summary>
    public string CurrentProviderStatus
    {
        get
        {
            if (Provider == "本地")
            {
                return $"本地后端 | 协议：{Protocol} | HTTP：{HttpBaseUrl}";
            }
            else
            {
                var keyStatus = string.IsNullOrEmpty(OpenAiApiKey) ? "未设置" : "已配置";
                return $"在线服务 | 模型：{OpenAiModel} | API Key：{keyStatus}";
            }
        }
    }

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
        Provider = s.Provider;

        OpenAiApiKey = s.OpenAiApiKey;
        OpenAiModel = s.OpenAiModel;
        OpenAiBaseUrl = s.OpenAiBaseUrl;
        OpenAiTemperature = s.OpenAiTemperature;
        OpenAiMaxTokens = s.OpenAiMaxTokens;

        OnPropertyChanged(nameof(ProviderConfigButtonText));
        OnPropertyChanged(nameof(CurrentProviderStatus));
    }

    [RelayCommand]
    public void SaveSettings()
    {
        _settingsService.Update(s =>
        {
            s.HttpBaseUrl = HttpBaseUrl;
            s.GrpcAddress = GrpcAddress;
            s.Protocol = Protocol;
            s.FontSize = FontSize;
            s.PerformanceMode = PerformanceMode;
            s.Theme = Theme;
            s.Provider = Provider;

            s.OpenAiApiKey = OpenAiApiKey;
            s.OpenAiModel = OpenAiModel;
            s.OpenAiBaseUrl = OpenAiBaseUrl;
            s.OpenAiTemperature = OpenAiTemperature;
            s.OpenAiMaxTokens = OpenAiMaxTokens;
        });

        OnPropertyChanged(nameof(ProviderConfigButtonText));
        OnPropertyChanged(nameof(CurrentProviderStatus));
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
        Provider = "本地";

        OpenAiApiKey = "";
        OpenAiModel = "gpt-3.5-turbo";
        OpenAiBaseUrl = "https://api.openai.com/v1";
        OpenAiTemperature = 0.7;
        OpenAiMaxTokens = 2048;

        SaveSettings();
    }

    /// <summary>
    /// 打开提供商配置窗口事件
    /// </summary>
    public event EventHandler<string>? OpenProviderConfigRequested;

    [RelayCommand]
    private void OpenProviderConfig()
    {
        OpenProviderConfigRequested?.Invoke(this, Provider);
    }

    /// <summary>
    /// 刷新状态显示（窗口关闭后调用）
    /// </summary>
    public void RefreshStatus()
    {
        OnPropertyChanged(nameof(ProviderConfigButtonText));
        OnPropertyChanged(nameof(CurrentProviderStatus));
    }
}
