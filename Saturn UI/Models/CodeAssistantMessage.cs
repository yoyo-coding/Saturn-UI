using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SaturnUI.Models;

public partial class CodeAssistantMessage : ObservableObject
{
    public CodeAssistantMessage(string role, string content)
    {
        Role = role;
        _content = content;
        CreatedAt = DateTime.Now;
    }

    public string Role { get; }

    public DateTime CreatedAt { get; }

    public bool IsUser => string.Equals(Role, "user", StringComparison.OrdinalIgnoreCase);

    public bool IsAssistant => string.Equals(Role, "assistant", StringComparison.OrdinalIgnoreCase);

    [ObservableProperty]
    private string _content = string.Empty;
}
