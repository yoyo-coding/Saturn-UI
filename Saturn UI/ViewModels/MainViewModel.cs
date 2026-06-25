using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;

namespace SaturnUI.ViewModels;

/// <summary>
/// 主视图模型 - 聚合子 ViewModel,管理主窗口全局状态
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    public ChatViewModel ChatViewModel { get; }
    public SessionListViewModel SessionListViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    [ObservableProperty]
    private bool _isSessionPanelOpen = true;

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private string _currentViewName = "Chat";

    public MainViewModel(
        ChatViewModel chatVm,
        SessionListViewModel sessionListVm,
        SettingsViewModel settingsVm)
    {
        ChatViewModel = chatVm;
        SessionListViewModel = sessionListVm;
        SettingsViewModel = settingsVm;

        SessionListViewModel.SessionSelected += OnSessionSelected;
    }

    private void OnSessionSelected(object? sender, Session session)
    {
        ChatViewModel.LoadSession(session);
        CurrentViewName = "Chat";
    }

    [RelayCommand]
    private void ToggleSessionPanel() => IsSessionPanelOpen = !IsSessionPanelOpen;

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    private void NewChat() => ChatViewModel.NewSession();

    protected override void DisposeCore()
    {
        SessionListViewModel.SessionSelected -= OnSessionSelected;
        ChatViewModel.Dispose();
        SettingsViewModel.Dispose();
    }
}
