namespace ChemSculptor.Domain;

public sealed class WorkflowRun
{
    public string Id { get; set; } = string.Empty;

    public WorkflowDefinition Definition { get; set; } = new WorkflowDefinition();

    public WorkflowState State { get; set; } = WorkflowState.Draft;

    public Dictionary<string, TaskState> NodeStates { get; set; } =
        new Dictionary<string, TaskState>(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, TaskResult> Results { get; set; } =
        new Dictionary<string, TaskResult>(StringComparer.OrdinalIgnoreCase);

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
