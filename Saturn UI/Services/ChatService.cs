using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SaturnUI.Services.Chat;

namespace SaturnUI.Services;

/// <summary>
/// 聊天服务 - 路由到对应协议提供器
/// 优化:
///   1. 协议实现拆分为独立 IChatProvider,便于扩展
///   2. 共享 HttpClient 实例,避免端口耗尽
///   3. 切换协议时无副作用
/// </summary>
public sealed class ChatService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly Dictionary<string, IChatProvider> _providers = new();
    private readonly HttpClient _sharedHttpClient;

    public ChatService(SettingsService settings)
    {
        _settings = settings;
        _sharedHttpClient = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        })
        {
            Timeout = TimeSpan.FromSeconds(300)
        };

        // 初始化所有提供器(后续按需切换)
        _providers[GrpcChatProvider.ProtocolName] = new GrpcChatProvider(
            () => _settings.Settings.GrpcAddress);
        _providers[HttpChatProvider.ProtocolName] = new HttpChatProvider(
            _sharedHttpClient,
            () => _settings.Settings.HttpBaseUrl);
        _providers[OpenAiChatProvider.ProtocolName] = new OpenAiChatProvider(
            _sharedHttpClient,
            () => new OpenAiConfig(
                _settings.Settings.OpenAiApiKey,
                _settings.Settings.OpenAiModel,
                _settings.Settings.OpenAiBaseUrl,
                _settings.Settings.OpenAiTemperature,
                _settings.Settings.OpenAiMaxTokens));
    }

    private IChatProvider GetProvider()
    {
        var protocol = _settings.Settings.Provider == "在线"
            ? OpenAiChatProvider.ProtocolName
            : _settings.Settings.Protocol;
        return _providers.TryGetValue(protocol, out var p)
            ? p
            : _providers[HttpChatProvider.ProtocolName];
    }

    public Task<string?> SendMessageAsync(
        string message, string? conversationId, CancellationToken ct = default)
        => GetProvider().SendAsync(message, conversationId, ct);

    public IAsyncEnumerable<string> SendMessageStreamAsync(
        string message, string? conversationId, CancellationToken ct = default)
        => GetProvider().SendStreamAsync(message, conversationId, ct);

    public void Dispose()
    {
        foreach (var p in _providers.Values)
            p.Dispose();
        _sharedHttpClient.Dispose();
    }
}
