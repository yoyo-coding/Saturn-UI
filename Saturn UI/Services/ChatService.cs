using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using SaturnUI.Models;
using SaturnUI.Protos;

namespace SaturnUI.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settings;
    private GrpcChannel? _grpcChannel;
    private Protos.ChatService.ChatServiceClient? _grpcClient;
    private string _lastGrpcAddress = string.Empty;

    public ChatService(SettingsService settings)
    {
        _settings = settings;
        _httpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        });
        _httpClient.Timeout = TimeSpan.FromSeconds(300);

        settings.SettingsChanged += (_, _) => ResetGrpcChannel();
    }

    private void ResetGrpcChannel()
    {
        _grpcChannel?.Dispose();
        _grpcChannel = null;
        _grpcClient = null;
        _lastGrpcAddress = string.Empty;
    }

    private Protos.ChatService.ChatServiceClient GetGrpcClient()
    {
        var address = _settings.Settings.GrpcAddress;
        if (_grpcClient != null && _lastGrpcAddress == address)
            return _grpcClient;

        _grpcChannel?.Dispose();
        _grpcChannel = GrpcChannel.ForAddress(address);
        _grpcClient = new Protos.ChatService.ChatServiceClient(_grpcChannel);
        _lastGrpcAddress = address;
        return _grpcClient;
    }

    public async Task<string?> SendMessageAsync(string message, string? conversationId, CancellationToken ct = default)
    {
        if (_settings.Settings.Protocol == "gRPC")
        {
            var client = GetGrpcClient();
            var request = new Protos.ChatRequest
            {
                Message = message,
                ConversationId = conversationId ?? ""
            };
            using var streamingCall = client.Chat(request, cancellationToken: ct);
            var reply = await streamingCall.ResponseStream.MoveNext(ct)
                ? streamingCall.ResponseStream.Current
                : null;
            return reply?.Content;
        }

        if (_settings.Settings.Protocol == "OpenAI")
        {
            return await SendMessageOpenAiAsync(message, conversationId, ct);
        }

        var url = GetUrl("/chat");
        var requestJson = new Models.ChatRequest
        {
            Message = message,
            ConversationId = conversationId,
            Stream = false
        };

        var json = JsonSerializer.Serialize(requestJson);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var responseHttp = await _httpClient.PostAsync(url, content, ct);
        responseHttp.EnsureSuccessStatusCode();

        var responseJson = await responseHttp.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<ChatResponse>(responseJson);
        return result?.Content;
    }

    public async IAsyncEnumerable<string> SendMessageStreamAsync(
        string message,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_settings.Settings.Protocol == "gRPC")
        {
            var client = GetGrpcClient();
            var request = new Protos.ChatRequest
            {
                Message = message,
                ConversationId = conversationId ?? ""
            };
            using var streamingCall = client.Chat(request, cancellationToken: ct);
            while (await streamingCall.ResponseStream.MoveNext(ct))
            {
                var reply = streamingCall.ResponseStream.Current;
                if (reply.Done) yield break;
                if (!string.IsNullOrEmpty(reply.Error))
                {
                    yield return $"[ERROR]{reply.Error}";
                    yield break;
                }
                if (!string.IsNullOrEmpty(reply.Token))
                    yield return reply.Token;
                else if (!string.IsNullOrEmpty(reply.Content))
                    yield return reply.Content;
            }
            yield break;
        }

        if (_settings.Settings.Protocol == "OpenAI")
        {
            await foreach (var token in SendMessageOpenAiStreamAsync(message, conversationId, ct))
                yield return token;
            yield break;
        }

        var url = GetUrl("/chat");
        var requestJson = new Models.ChatRequest
        {
            Message = message,
            ConversationId = conversationId,
            Stream = true
        };

        var json = JsonSerializer.Serialize(requestJson);
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
            try
            {
                parsed = JsonSerializer.Deserialize<ChatResponse>(data);
            }
            catch { }

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

    private string GetUrl(string path)
    {
        var baseUrl = _settings.Settings.HttpBaseUrl.TrimEnd('/');
        return $"{baseUrl}{path}";
    }

    // ==================== OpenAI 兼容 API ====================

    /// <summary>
    /// 获取 OpenAI API 端点 URL
    /// </summary>
    private string GetOpenAiUrl()
    {
        var baseUrl = _settings.Settings.OpenAiBaseUrl.TrimEnd('/');
        return $"{baseUrl}/chat/completions";
    }

    /// <summary>
    /// 构建 OpenAI 请求头（包含 API Key 认证）
    /// </summary>
    private HttpRequestMessage BuildOpenAiRequest(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, GetOpenAiUrl())
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // 添加 API Key 认证头
        var apiKey = _settings.Settings.OpenAiApiKey;
        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        return request;
    }

    /// <summary>
    /// 处理 OpenAI API 错误响应
    /// </summary>
    private string HandleOpenAiError(HttpResponseMessage response, string responseBody)
    {
        var statusCode = (int)response.StatusCode;

        // 尝试解析错误信息
        string? errorMessage = null;
        try
        {
            var error = JsonSerializer.Deserialize<OpenAiError>(responseBody);
            errorMessage = error?.Message;
        }
        catch { }

        var prefix = $"[ERROR]";

        return statusCode switch
        {
            401 => $"{prefix}认证失败：请检查 API Key 是否正确",
            403 => $"{prefix}访问被拒绝：API Key 无权限",
            404 => $"{prefix}端点不存在：请检查 Base URL 配置",
            429 => $"{prefix}请求过于频繁：请稍后重试",
            >= 500 => $"{prefix}服务端错误 ({statusCode}){(errorMessage != null ? $": {errorMessage}" : "")}",
            _ => $"{prefix}请求失败 ({statusCode}){(errorMessage != null ? $": {errorMessage}" : "")}"
        };
    }

    /// <summary>
    /// OpenAI 非流式调用
    /// </summary>
    public async Task<string?> SendMessageOpenAiAsync(string message, string? conversationId, CancellationToken ct = default)
    {
        var settings = _settings.Settings;

        if (string.IsNullOrEmpty(settings.OpenAiApiKey))
        {
            return "[ERROR]请先在设置中配置 OpenAI API Key";
        }

        var request = new OpenAiChatRequest
        {
            Model = settings.OpenAiModel,
            Temperature = settings.OpenAiTemperature,
            MaxTokens = settings.OpenAiMaxTokens,
            Stream = false,
            Messages = new List<OpenAiMessage>
            {
                new() { Role = "user", Content = message }
            }
        };

        var json = JsonSerializer.Serialize(request);
        using var httpRequest = BuildOpenAiRequest(json);

        try
        {
            var response = await _httpClient.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                return HandleOpenAiError(response, responseBody);
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<OpenAiChatResponse>(responseJson);

            if (result?.Error != null)
            {
                return $"[ERROR]{result.Error.Message}";
            }

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
        catch (Exception ex)
        {
            return $"[ERROR]未知错误：{ex.Message}";
        }
    }

    /// <summary>
    /// OpenAI 流式调用（SSE）
    /// </summary>
    public async IAsyncEnumerable<string> SendMessageOpenAiStreamAsync(
        string message,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var settings = _settings.Settings;

        if (string.IsNullOrEmpty(settings.OpenAiApiKey))
        {
            yield return "[ERROR]请先在设置中配置 OpenAI API Key";
            yield break;
        }

        var request = new OpenAiChatRequest
        {
            Model = settings.OpenAiModel,
            Temperature = settings.OpenAiTemperature,
            MaxTokens = settings.OpenAiMaxTokens,
            Stream = true,
            Messages = new List<OpenAiMessage>
            {
                new() { Role = "user", Content = message }
            }
        };

        var json = JsonSerializer.Serialize(request);
        using var httpRequest = BuildOpenAiRequest(json);

        HttpResponseMessage? response = null;
        string? httpError = null;

        // 发送请求（不在 try-catch 中 yield）
        try
        {
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                httpError = HandleOpenAiError(response, responseBody);
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

        // 读取 SSE 流（无 catch 的 try 块允许 yield）
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
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAiStreamChunk>(data);
                }
                catch { continue; }

                if (chunk?.Choices == null || chunk.Choices.Count == 0) continue;

                var choice = chunk.Choices[0];

                if (choice.FinishReason != null)
                    yield break;

                if (!string.IsNullOrEmpty(choice.Delta?.Content))
                    yield return choice.Delta.Content;
            }
        }
    }
}
