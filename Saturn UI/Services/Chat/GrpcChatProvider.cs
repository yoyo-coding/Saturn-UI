using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using SaturnUI.Protos;
using ProtoChatService = SaturnUI.Protos.ChatService;

namespace SaturnUI.Services.Chat;

/// <summary>
/// gRPC 协议提供器
/// </summary>
public sealed class GrpcChatProvider : IChatProvider, IDisposable
{
    public const string ProtocolName = "gRPC";

    private readonly Func<string> _addressProvider;
    private GrpcChannel? _channel;
    private ProtoChatService.ChatServiceClient? _client;
    private string _lastAddress = string.Empty;
    private readonly object _lock = new();

    public string Name => ProtocolName;

    public GrpcChatProvider(Func<string> addressProvider)
    {
        _addressProvider = addressProvider;
    }

    private ProtoChatService.ChatServiceClient GetClient()
    {
        var address = _addressProvider();
        lock (_lock)
        {
            if (_client != null && _lastAddress == address)
                return _client;

            _channel?.Dispose();
            _channel = GrpcChannel.ForAddress(address);
            _client = new ProtoChatService.ChatServiceClient(_channel);
            _lastAddress = address;
            return _client;
        }
    }

    public async Task<string?> SendAsync(string message, string? conversationId, CancellationToken ct = default)
    {
        var client = GetClient();
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = conversationId ?? ""
        };
        using var call = client.Chat(request, cancellationToken: ct);
        var reply = await call.ResponseStream.MoveNext(ct)
            ? call.ResponseStream.Current
            : null;
        return reply?.Content;
    }

    public async IAsyncEnumerable<string> SendStreamAsync(
        string message,
        string? conversationId,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var client = GetClient();
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = conversationId ?? ""
        };

        using var call = client.Chat(request, cancellationToken: ct);
        while (await call.ResponseStream.MoveNext(ct))
        {
            var reply = call.ResponseStream.Current;
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
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _channel?.Dispose();
            _channel = null;
            _client = null;
        }
    }
}
