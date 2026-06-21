using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SaturnUI.Models;

public partial class Session : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _title = "新会话";

    [ObservableProperty]
    private DateTime _createdAt = DateTime.Now;

    [ObservableProperty]
    private DateTime _updatedAt = DateTime.Now;

    [ObservableProperty]
    private ObservableCollection<Message> _messages = new();

    public Session() { }

    public Session(string title)
    {
        _title = title;
    }
}
