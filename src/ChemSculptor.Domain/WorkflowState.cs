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

public sealed record WorkflowEvent
{
    public required string Type { get; init; }

    public required string WorkflowId { get; init; }

    public string? NodeId { get; init; }

    public string? Payload { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
