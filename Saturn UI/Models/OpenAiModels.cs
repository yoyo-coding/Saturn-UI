using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SaturnUI.Models;

/// <summary>
/// OpenAI Chat Completions API 请求模型
/// 兼容 OpenAI 官方 API、Azure OpenAI、Ollama、vLLM、LM Studio 等
/// </summary>
public class OpenAiChatRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = "gpt-3.5-turbo";

    [JsonPropertyName("messages")]
    public List<OpenAiMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 2048;

    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;
}

/// <summary>
/// OpenAI 消息格式
/// </summary>
public class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI Chat Completions API 响应模型（非流式）
/// </summary>
public class OpenAiChatResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAiChoice>? Choices { get; set; }

    [JsonPropertyName("usage")]
    public OpenAiUsage? Usage { get; set; }

    [JsonPropertyName("error")]
    public OpenAiError? Error { get; set; }
}

/// <summary>
/// OpenAI 响应选项
/// </summary>
public class OpenAiChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("message")]
    public OpenAiMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// OpenAI Token 使用情况
/// </summary>
public class OpenAiUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// OpenAI 错误响应
/// </summary>
public class OpenAiError
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

/// <summary>
/// OpenAI 流式响应块（SSE）
/// </summary>
public class OpenAiStreamChunk
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("choices")]
    public List<OpenAiStreamChoice>? Choices { get; set; }
}

/// <summary>
/// OpenAI 流式响应选项
/// </summary>
public class OpenAiStreamChoice
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("delta")]
    public OpenAiDelta? Delta { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}

/// <summary>
/// OpenAI 流式响应增量内容
/// </summary>
public class OpenAiDelta
{
    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
