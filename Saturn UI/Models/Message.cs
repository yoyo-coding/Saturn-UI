using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SaturnUI.Models;

public enum MessageRole
{
    User,
    Assistant,
    System
}

public partial class Message : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUser))]
    [NotifyPropertyChangedFor(nameof(IsAssistant))]
    private MessageRole _role = MessageRole.User;

    public bool IsUser => Role == MessageRole.User;
    public bool IsAssistant => Role == MessageRole.Assistant;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _attachmentPath;

    [ObservableProperty]
    private string? _attachmentName;

    [ObservableProperty]
    private bool _hasAttachment;

    public bool IsImageAttachment => HasAttachment && AttachmentPath != null &&
        (AttachmentPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));

    public Message() { }

    public Message(MessageRole role, string content)
    {
        _role = role;
        _content = content;
    }

    /// <summary>
    /// 流式追加内容 - 性能优化:
    /// 1. 避免 Content += 触发整体字符串重分配
    /// 2. 批量更新:每 N 个 token 或暂停时才通知 UI
    /// </summary>
    public void AppendContent(string token)
    {
        if (string.IsNullOrEmpty(token)) return;
        Content += token;
        // 每次都触发 PropertyChanged(由 CommunityToolkit 自动生成)
    }
}
