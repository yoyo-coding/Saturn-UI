using System;
using System.IO;
using System.Net.Http;
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
}
