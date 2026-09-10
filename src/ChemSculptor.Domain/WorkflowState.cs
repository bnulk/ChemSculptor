namespace ChemSculptor.Domain;

public enum WorkflowState
{
    Draft,
    Ready,
    Running,
    WaitingValidation,
    Passed,
    Failed,
    Recovering,
    AwaitingApproval,
    Canceled,
    Suspended,
    Archived
}

public enum TaskState
{
    Pending,
    Running,
    WaitingValidation,
    Passed,
    Failed,
    Recovering,
    AwaitingApproval,
    Canceled
}

public static class WorkflowEventTypes
{
    public const string WorkflowStarted = "workflow.started";
    public const string WorkflowCompleted = "workflow.completed";
    public const string WorkflowFailed = "workflow.failed";
    public const string TaskStarted = "task.started";
    public const string TaskCompleted = "task.completed";
    public const string TaskFailed = "task.failed";
}

public sealed class WorkflowEvent
{
    public string Type { get; set; } = string.Empty;

    public string WorkflowId { get; set; } = string.Empty;

    public string? NodeId { get; set; }

    public string? Payload { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
