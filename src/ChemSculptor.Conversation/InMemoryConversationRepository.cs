using System.Collections.Concurrent;

namespace ChemSculptor.Conversation;

/// <summary>
/// 内存版会话仓储。
/// 仅用于当前开发阶段，重启后数据会丢失。
/// </summary>
public sealed class InMemoryConversationRepository : IConversationRepository
{
    private readonly ConcurrentDictionary<string, ConversationSession> _sessions =
        new ConcurrentDictionary<string, ConversationSession>(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, List<ConversationMessage>> _messages =
        new ConcurrentDictionary<string, List<ConversationMessage>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>保存或更新会话。</summary>
    public Task SaveSessionAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        session.UpdatedAt = DateTimeOffset.UtcNow;
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }

    /// <summary>读取会话。</summary>
    public Task<ConversationSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ConversationSession? session;
        if (_sessions.TryGetValue(sessionId, out session))
        {
            return Task.FromResult<ConversationSession?>(session);
        }

        return Task.FromResult<ConversationSession?>(null);
    }

    /// <summary>追加一条消息。</summary>
    public Task AppendMessageAsync(
        ConversationMessage message,
        CancellationToken cancellationToken = default)
    {
        List<ConversationMessage> emptyList = new List<ConversationMessage>();
        List<ConversationMessage> messages = _messages.GetOrAdd(message.SessionId, emptyList);

        lock (messages)
        {
            messages.Add(message);
        }

        return Task.CompletedTask;
    }

    /// <summary>读取会话的全部消息。</summary>
    public Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        List<ConversationMessage> result = new List<ConversationMessage>();
        List<ConversationMessage>? messages;

        if (_messages.TryGetValue(sessionId, out messages))
        {
            lock (messages)
            {
                result.AddRange(messages);
            }
        }

        return Task.FromResult<IReadOnlyList<ConversationMessage>>(result);
    }
}
