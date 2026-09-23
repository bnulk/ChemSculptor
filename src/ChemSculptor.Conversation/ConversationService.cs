using ChemSculptor.Compute;

namespace ChemSculptor.Conversation;

/// <summary>
/// 会话服务。
/// 负责保存消息、解释任务意图并生成结构化回复。
/// </summary>
public sealed class ConversationService : IConversationService
{
    private readonly ITaskInterpreter _taskInterpreter;
    private readonly IConversationRepository _repository;

    /// <summary>创建会话服务。</summary>
    public ConversationService(
        ITaskInterpreter taskInterpreter,
        IConversationRepository repository)
    {
        _taskInterpreter = taskInterpreter;
        _repository = repository;
    }

    /// <summary>处理一条用户消息。</summary>
    public async Task<ConversationReply> HandleMessageAsync(
        ConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        string sessionId = request.SessionId;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            sessionId = "session-" + Guid.NewGuid().ToString("N");
        }

        ConversationSession? session = await _repository.GetSessionAsync(sessionId, cancellationToken);

        if (session == null)
        {
            session = new ConversationSession();
            session.SessionId = sessionId;
            session.Title = "新会话";
            session.WorkflowId = "workflow-" + Guid.NewGuid().ToString("N");
            await _repository.SaveSessionAsync(session, cancellationToken);
        }

        ConversationMessage userMessage = new ConversationMessage();
        userMessage.MessageId = "message-" + Guid.NewGuid().ToString("N");
        userMessage.SessionId = sessionId;
        userMessage.Role = ConversationMessageRole.User;
        userMessage.MessageType = ConversationMessageType.Text;
        userMessage.Text = request.Text;
        await _repository.AppendMessageAsync(userMessage, cancellationToken);

        InterpretedTask interpretedTask =
            await _taskInterpreter.InterpretAsync(request.Text, cancellationToken);

        ConversationIntent intent = new ConversationIntent();
        intent.TaskType = interpretedTask.TaskType;
        intent.WorkflowId = interpretedTask.WorkflowId;
        intent.IsSupported = interpretedTask.IsSupported;
        intent.Confidence = interpretedTask.Confidence;
        intent.Diagnostics = new List<string>(interpretedTask.Diagnostics);

        ConversationReply reply = new ConversationReply();
        reply.Intent = intent;

        if (intent.IsSupported)
        {
            reply.ReplyMessage = "已识别任务类型：单点计算。";
            reply.ReplyType = ConversationReplyType.TaskAccepted;
        }
        else
        {
            reply.ReplyMessage = "当前阶段只支持单点计算。";
            reply.ReplyType = ConversationReplyType.Error;
            reply.Diagnostics = new List<string>(intent.Diagnostics);
        }

        ConversationMessage agentMessage = new ConversationMessage();
        agentMessage.MessageId = "message-" + Guid.NewGuid().ToString("N");
        agentMessage.SessionId = sessionId;
        agentMessage.Role = ConversationMessageRole.Agent;
        agentMessage.MessageType = ConversationMessageType.Text;
        agentMessage.Text = reply.ReplyMessage;
        await _repository.AppendMessageAsync(agentMessage, cancellationToken);

        return reply;
    }
}
