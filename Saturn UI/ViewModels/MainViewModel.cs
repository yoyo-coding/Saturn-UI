using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;

namespace SaturnUI.ViewModels;

/// <summary>
/// ????????????????????????
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
        ChatViewModel.SessionCreated += OnSessionCreated;
    }

    private void OnSessionSelected(object? sender, Session session)
    {
        ChatViewModel.LoadSession(session);
        CurrentViewName = "Chat";
        IsSettingsOpen = false;
    }

    private void OnSessionCreated(object? sender, Session session)
        => SessionListViewModel.AddOrRefreshSession(session);

    [RelayCommand]
    private void ToggleSessionPanel() => IsSessionPanelOpen = !IsSessionPanelOpen;

    [RelayCommand]
    private void OpenSettings() => IsSettingsOpen = true;

    [RelayCommand]
    private void CloseSettings() => IsSettingsOpen = false;

    [RelayCommand]
    private void NewChat() => SessionListViewModel.NewSessionCommand.Execute(null);

    protected override void DisposeCore()
    {
        SessionListViewModel.SessionSelected -= OnSessionSelected;
        ChatViewModel.SessionCreated -= OnSessionCreated;
        ChatViewModel.Dispose();
        SettingsViewModel.Dispose();
    }
}
