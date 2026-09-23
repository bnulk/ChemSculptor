namespace ChemSculptor.Conversation;

/// <summary>
/// 会话仓储接口。
/// </summary>
public interface IConversationRepository
{
    /// <summary>保存或更新会话。</summary>
    Task SaveSessionAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default);

    /// <summary>读取会话。</summary>
    Task<ConversationSession?> GetSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>追加一条消息。</summary>
    Task AppendMessageAsync(
        ConversationMessage message,
        CancellationToken cancellationToken = default);

    /// <summary>读取会话的全部消息。</summary>
    Task<IReadOnlyList<ConversationMessage>> GetMessagesAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 会话服务接口。
/// 负责接收用户原始消息并返回结构化回复。
/// </summary>
public interface IConversationService
{
    /// <summary>处理一条用户消息。</summary>
    Task<ConversationReply> HandleMessageAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default);
}
