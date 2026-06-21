using System.Text.Json.Serialization;

namespace SaturnUI.Models;

public class ChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("conversation_id")]
    public string? ConversationId { get; set; }

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = true;
}

public class ChatResponse
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("done")]
    public bool Done { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
