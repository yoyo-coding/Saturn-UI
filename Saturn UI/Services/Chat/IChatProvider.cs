using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SaturnUI.Services.Chat;

/// <summary>
/// 聊天后端协议提供器接口
/// 每种协议(gRPC / HTTP / OpenAI 等)实现此接口,ChatService 负责路由
/// </summary>
public interface IChatProvider : IDisposable
{
    /// <summary>
    /// 提供器名称(对应 Settings.Protocol)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 非流式调用
    /// </summary>
    Task<string?> SendAsync(string message, string? conversationId, CancellationToken ct = default);

    /// <summary>
    /// 流式调用(SSE 风格)
    /// </summary>
    IAsyncEnumerable<string> SendStreamAsync(
        string message,
        string? conversationId,
        CancellationToken ct = default);
}
