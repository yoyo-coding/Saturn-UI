using System;
using System.Diagnostics;
using System.Text;
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
    private const int StreamingFlushIntervalMs = 33;
    private const int StreamingFlushTokenChars = 24;

    private readonly StringBuilder _streamBuffer = new();
    private long _lastFlushTimestamp;

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
    [NotifyPropertyChangedFor(nameof(IsImageAttachment))]
    private bool _hasAttachment;

    public bool IsImageAttachment => HasAttachment && AttachmentPath != null &&
        (AttachmentPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
         AttachmentPath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));

    partial void OnAttachmentPathChanged(string? value) => OnPropertyChanged(nameof(IsImageAttachment));

    public Message() { }

    public Message(MessageRole role, string content)
    {
        _role = role;
        _content = content;
    }

    /// <summary>
    /// ?????????? Markdown ???????????????????/?????????
    /// </summary>
    public void AppendContent(string token)
    {
        if (string.IsNullOrEmpty(token)) return;

        if (!IsStreaming)
        {
            Content += token;
            return;
        }

        _streamBuffer.Append(token);
        if (_lastFlushTimestamp == 0)
            _lastFlushTimestamp = Stopwatch.GetTimestamp();

        var elapsed = Stopwatch.GetElapsedTime(_lastFlushTimestamp);
        if (_streamBuffer.Length >= StreamingFlushTokenChars || elapsed.TotalMilliseconds >= StreamingFlushIntervalMs)
            FlushContentBuffer();
    }

    public void CompleteStreaming()
    {
        FlushContentBuffer();
        IsStreaming = false;
    }

    public void FlushContentBuffer()
    {
        if (_streamBuffer.Length == 0) return;
        Content += _streamBuffer.ToString();
        _streamBuffer.Clear();
        _lastFlushTimestamp = Stopwatch.GetTimestamp();
    }
}
