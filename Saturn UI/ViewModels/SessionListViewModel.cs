using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaturnUI.Models;
using SaturnUI.Services;

namespace SaturnUI.ViewModels;

public partial class SessionListViewModel : ViewModelBase
{
    private readonly LocalStorageService _storage;

    [ObservableProperty]
    private ObservableCollection<Session> _sessions = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Session? _selectedSession;

    public event EventHandler<Session>? SessionSelected;

    public SessionListViewModel(LocalStorageService storage)
    {
        _storage = storage;
        LoadSessions();
    }

    public void LoadSessions()
    {
        var list = _storage.GetSessions();
        Sessions = new ObservableCollection<Session>(list);
    }

    [RelayCommand]
    private void SelectSession(Session session)
    {
        SelectedSession = session;
        var full = _storage.GetSession(session.Id);
        if (full != null)
            SessionSelected?.Invoke(this, full);
    }

    [RelayCommand]
    private void DeleteSession(Session session)
    {
        _storage.DeleteSession(session.Id);
        Sessions.Remove(session);
        if (SelectedSession?.Id == session.Id)
            SelectedSession = null;
    }

    [RelayCommand]
    private void SearchSessions()
    {
        var all = _storage.GetSessions();
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? all
            : all.Where(s => s.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        Sessions = new ObservableCollection<Session>(filtered);
    }

    [RelayCommand]
    private void NewSession()
    {
        var session = new Session("新会话");
        _storage.SaveSession(session);
        Sessions.Insert(0, session);
        SelectSession(session);
    }
}
