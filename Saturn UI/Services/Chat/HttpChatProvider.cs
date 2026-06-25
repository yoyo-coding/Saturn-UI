using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SaturnUI.Models;

namespace SaturnUI.Services.Chat;

/// <summary>
/// 自定义 HTTP JSON 协议提供器
/// </summary>
public sealed class HttpChatProvider : IChatProvider
{
    public const string ProtocolName = "HTTP";

    private readonly HttpClient _httpClient;
    private readonly Func<string> _baseUrlProvider;

    public string Name => ProtocolName;

    public HttpChatProvider(HttpClient httpClient, Func<string> baseUrlProvider)
    {
        _httpClient = httpClient;
        _baseUrlProvider = baseUrlProvider;
    }

    public async Task<string?> SendAsync(string message, string? conversationId, CancellationToken ct = default)
    {
        var url = GetUrl("/chat");
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = conversationId,
            Stream = false
        };

        var response = await PostAsync(url, request, ct);
        var result = JsonSerializer.Deserialize<ChatResponse>(response);
        return result?.Content;
    }

    public async IAsyncEnumerable<string> SendStreamAsync(
        string message,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = GetUrl("/chat");
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = conversationId,
            Stream = true
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("data:")) continue;

            var data = line.AsSpan(5).Trim().ToString();
            if (data == "[DONE]") yield break;

            ChatResponse? parsed = null;
            try { parsed = JsonSerializer.Deserialize<ChatResponse>(data); }
            catch { continue; }

            if (parsed?.Done == true) yield break;
            if (parsed?.Error != null)
            {
                yield return $"[ERROR]{parsed.Error}";
                yield break;
            }

            if (!string.IsNullOrEmpty(parsed?.Token))
                yield return parsed.Token;
            else if (!string.IsNullOrEmpty(parsed?.Content))
                yield return parsed.Content;
        }
    }

    private async Task<string> PostAsync(string url, ChatRequest body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private string GetUrl(string path)
    {
        var baseUrl = _baseUrlProvider().TrimEnd('/');
        return $"{baseUrl}{path}";
    }

    public void Dispose() { /* HTTP client 由 ChatService 统一管理 */ }
}
