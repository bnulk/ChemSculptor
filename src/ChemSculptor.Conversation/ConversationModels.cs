using ChemSculptor.Compute;

namespace ChemSculptor.Conversation;

/// <summary>消息发送者角色。</summary>
public enum ConversationMessageRole
{
    /// <summary>用户。</summary>
    User,

    /// <summary>智能体。</summary>
    Agent,

    /// <summary>系统。</summary>
    System
}

/// <summary>消息类型。</summary>
public enum ConversationMessageType
{
    /// <summary>普通文本。</summary>
    Text,

    /// <summary>向用户提问。</summary>
    Question,

    /// <summary>审批请求。</summary>
    ApprovalRequest,

    /// <summary>任务状态。</summary>
    TaskStatus,

    /// <summary>结果。</summary>
    Result,

    /// <summary>错误。</summary>
    Error
}

/// <summary>回复类型。</summary>
public enum ConversationReplyType
{
    /// <summary>普通回答。</summary>
    Answer,

    /// <summary>需要用户澄清。</summary>
    Clarification,

    /// <summary>任务已接受。</summary>
    TaskAccepted,

    /// <summary>任务状态更新。</summary>
    TaskStatus,

    /// <summary>结果回复。</summary>
    Result,

    /// <summary>错误回复。</summary>
    Error
}

/// <summary>一次会话。</summary>
public sealed class ConversationSession
{
    /// <summary>会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>会话标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>最后更新时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>关联的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;
}

/// <summary>会话中的一条消息。</summary>
public sealed class ConversationMessage
{
    /// <summary>消息标识。</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>所属会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>发送者角色。</summary>
    public ConversationMessageRole Role { get; set; } = ConversationMessageRole.User;

    /// <summary>消息类型。</summary>
    public ConversationMessageType MessageType { get; set; } = ConversationMessageType.Text;

    /// <summary>消息文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>关联的请求或任务标识。</summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>关联的计算任务标识。</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>关联的计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;
}

/// <summary>发送给会话服务的用户消息。</summary>
public sealed class ConversationRequest
{
    /// <summary>会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>用户原始文本。</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>会话解释出的任务意图。</summary>
public sealed class ConversationIntent
{
    /// <summary>任务类型；无法识别时为空。</summary>
    public CalculationTaskType? TaskType { get; set; }

    /// <summary>建议使用的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>当前阶段是否支持。</summary>
    public bool IsSupported { get; set; }

    /// <summary>解释置信度。</summary>
    public double Confidence { get; set; }

    /// <summary>解释诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>需要用户回答的问题。</summary>
public sealed class ConversationQuestion
{
    /// <summary>问题标识。</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>问题类型。</summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>问题文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>可选项。</summary>
    public List<string> Options { get; set; } = new List<string>();

    /// <summary>风险等级。</summary>
    public CalculationRiskLevel RiskLevel { get; set; } = CalculationRiskLevel.Info;
}

/// <summary>对一条用户消息的回复。</summary>
public sealed class ConversationReply
{
    /// <summary>给用户看的回复文本。</summary>
    public string ReplyMessage { get; set; } = string.Empty;

    /// <summary>回复类型。</summary>
    public ConversationReplyType ReplyType { get; set; } = ConversationReplyType.Answer;

    /// <summary>是否需要用户操作。</summary>
    public bool RequiresUserAction { get; set; }

    /// <summary>解释出的任务意图。</summary>
    public ConversationIntent Intent { get; set; } = new ConversationIntent();

    /// <summary>需要用户回答的问题。</summary>
    public List<ConversationQuestion> Questions { get; set; } = new List<ConversationQuestion>();

    /// <summary>关联的计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>产生的文件或产物路径。</summary>
    public List<string> Artifacts { get; set; } = new List<string>();

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}
