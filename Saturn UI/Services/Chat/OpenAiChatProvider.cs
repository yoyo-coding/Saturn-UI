using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SaturnUI.Models;

namespace SaturnUI.Services.Chat;

/// <summary>
/// OpenAI 兼容 API 协议提供器
/// 支持 OpenAI 官方、Azure OpenAI、vLLM、LM Studio 等
/// </summary>
public sealed class OpenAiChatProvider : IChatProvider
{
    public const string ProtocolName = "OpenAI";

    private readonly HttpClient _httpClient;
    private readonly Func<OpenAiConfig> _configProvider;

    public string Name => ProtocolName;

    public OpenAiChatProvider(HttpClient httpClient, Func<OpenAiConfig> configProvider)
    {
        _httpClient = httpClient;
        _configProvider = configProvider;
    }

    public async Task<string?> SendAsync(string message, string? conversationId, CancellationToken ct = default)
    {
        var config = _configProvider();

        if (string.IsNullOrEmpty(config.ApiKey))
            return "[ERROR]请先在设置中配置 OpenAI API Key";

        var request = new OpenAiChatRequest
        {
            Model = config.Model,
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            Stream = false,
            Messages = new List<OpenAiMessage> { new() { Role = "user", Content = message } }
        };

        var json = JsonSerializer.Serialize(request);
        using var httpRequest = BuildRequest(json, config);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                return ErrorMapper.ToErrorMessage(response, body);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson);

            if (result?.Error != null)
                return $"[ERROR]{result.Error.Message}";

            return result?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (HttpRequestException ex)
        {
            return $"[ERROR]网络请求失败：{ex.Message}";
        }
        catch (TaskCanceledException)
        {
            return "[ERROR]请求超时，请检查网络连接";
        }
    }

    public async IAsyncEnumerable<string> SendStreamAsync(
        string message,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var config = _configProvider();

        if (string.IsNullOrEmpty(config.ApiKey))
        {
            yield return "[ERROR]请先在设置中配置 OpenAI API Key";
            yield break;
        }

        var request = new OpenAiChatRequest
        {
            Model = config.Model,
            Temperature = config.Temperature,
            MaxTokens = config.MaxTokens,
            Stream = true,
            Messages = new List<OpenAiMessage> { new() { Role = "user", Content = message } }
        };

        var json = JsonSerializer.Serialize(request);
        using var httpRequest = BuildRequest(json, config);

        HttpResponseMessage? response = null;
        string? httpError = null;

        try
        {
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                httpError = ErrorMapper.ToErrorMessage(response, body);
            }
        }
        catch (HttpRequestException ex)
        {
            httpError = $"网络请求失败：{ex.Message}";
        }
        catch (TaskCanceledException)
        {
            httpError = "请求超时，请检查网络连接";
        }
        catch (Exception ex)
        {
            httpError = $"未知错误：{ex.Message}";
        }

        if (httpError != null)
        {
            yield return $"[ERROR]{httpError}";
            yield break;
        }

        if (response != null)
        {
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

                OpenAiStreamChunk? chunk = null;
                try { chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data); }
                catch { continue; }

                if (chunk?.Choices == null || chunk.Choices.Count == 0) continue;

                var choice = chunk.Choices[0];
                if (choice.FinishReason != null) yield break;

                if (!string.IsNullOrEmpty(choice.Delta?.Content))
                    yield return choice.Delta.Content;
            }
        }
    }

    private HttpRequestMessage BuildRequest(string json, OpenAiConfig config)
    {
        var url = $"{config.BaseUrl.TrimEnd('/')}/chat/completions";
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(config.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        }

        return request;
    }

    public void Dispose() { /* HTTP client 由 ChatService 统一管理 */ }
}

/// <summary>
/// OpenAI 配置快照(不可变)
/// </summary>
public sealed record OpenAiConfig(
    string ApiKey,
    string Model,
    string BaseUrl,
    double Temperature,
    int MaxTokens);

/// <summary>
/// OpenAI 错误码 → 中文消息映射
/// </summary>
internal static class ErrorMapper
{
    public static string ToErrorMessage(HttpResponseMessage response, string responseBody)
    {
        string? errorMessage = null;
        try
        {
            var error = JsonSerializer.Deserialize<OpenAiError>(responseBody);
            errorMessage = error?.Message;
        }
        catch { /* ignore parse error */ }

        var statusCode = (int)response.StatusCode;
        var detail = errorMessage != null ? $": {errorMessage}" : string.Empty;

        return statusCode switch
        {
            401 => $"[ERROR]认证失败：请检查 API Key 是否正确",
            403 => $"[ERROR]访问被拒绝：API Key 无权限",
            404 => $"[ERROR]端点不存在：请检查 Base URL 配置",
            429 => $"[ERROR]请求过于频繁：请稍后重试",
            >= 500 => $"[ERROR]服务端错误 ({statusCode}){detail}",
            _ => $"[ERROR]请求失败 ({statusCode}){detail}"
        };
    }
}
