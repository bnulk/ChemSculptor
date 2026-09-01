namespace ChemSculptor.Domain;

public sealed class WorkflowRun
{
    public required string Id { get; init; }

    public required WorkflowDefinition Definition { get; init; }

    public WorkflowState State { get; set; } = WorkflowState.Draft;

    public Dictionary<string, TaskState> NodeStates { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, TaskResult> Results { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
