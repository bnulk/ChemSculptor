namespace ChemSculptor.Domain;

/// <summary>
/// 工作流整体状态。
/// 状态决定工作流当前处于哪个阶段，以及接下来允许发生什么动作。
/// </summary>
public enum WorkflowState
{
    /// <summary>草稿状态，尚未通过规则校验。</summary>
    Draft,

    /// <summary>已就绪，可以开始执行。</summary>
    Ready,

    /// <summary>正在执行。</summary>
    Running,

    /// <summary>等待验证门给出结果。</summary>
    WaitingValidation,

    /// <summary>全部节点执行并通过验证。</summary>
    Passed,

    /// <summary>执行失败。</summary>
    Failed,

    /// <summary>正在尝试恢复。</summary>
    Recovering,

    /// <summary>等待人工审批。</summary>
    AwaitingApproval,

    /// <summary>已取消。</summary>
    Canceled,

    /// <summary>已挂起，可后续恢复。</summary>
    Suspended,

    /// <summary>已归档。</summary>
    Archived
}

/// <summary>
/// 单个节点的执行状态。
/// </summary>
public enum TaskState
{
    /// <summary>尚未开始。</summary>
    Pending,

    /// <summary>正在执行。</summary>
    Running,

    /// <summary>等待验证。</summary>
    WaitingValidation,

    /// <summary>执行并通过验证。</summary>
    Passed,

    /// <summary>执行失败或验证失败。</summary>
    Failed,

    /// <summary>正在恢复。</summary>
    Recovering,

    /// <summary>等待人工审批。</summary>
    AwaitingApproval,

    /// <summary>已取消。</summary>
    Canceled
}

/// <summary>
/// 工作流事件类型常量。
/// 使用常量而不是散落的字符串，避免拼写错误并便于统一修改。
/// </summary>
public static class WorkflowEventTypes
{
    public const string WorkflowStarted = "workflow.started";
    public const string WorkflowCompleted = "workflow.completed";
    public const string WorkflowFailed = "workflow.failed";
    public const string TaskStarted = "task.started";
    public const string TaskCompleted = "task.completed";
    public const string TaskFailed = "task.failed";
}

/// <summary>
/// 一条工作流事件记录。
/// 事件同时用于实时通知和持久化日志，支持后续回放与审计。
/// </summary>
public sealed class WorkflowEvent
{
    /// <summary>事件类型，取值来自 <see cref="WorkflowEventTypes"/>。</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>事件所属的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>事件关联的节点标识；工作流级事件可以为空。</summary>
    public string? NodeId { get; set; }

    /// <summary>附加信息，例如失败原因或审批说明。</summary>
    public string? Payload { get; set; }

    /// <summary>事件发生时间。</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
