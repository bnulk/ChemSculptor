namespace ChemSculptor.Domain;

/// <summary>
/// 一次工作流执行的运行档案。
/// 定义（<see cref="WorkflowDefinition"/>）是不变的模板，运行档案记录本次执行的状态与结果。
/// </summary>
public sealed class WorkflowRun
{
    /// <summary>本次运行实例的标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>本次运行依据的工作流定义。</summary>
    public WorkflowDefinition Definition { get; set; } = new WorkflowDefinition();

    /// <summary>工作流整体状态。</summary>
    public WorkflowState State { get; set; } = WorkflowState.Draft;

    /// <summary>各节点的当前状态，键为节点标识。</summary>
    public Dictionary<string, TaskState> NodeStates { get; set; } =
        new Dictionary<string, TaskState>(StringComparer.OrdinalIgnoreCase);

    /// <summary>各节点的执行结果，键为节点标识。</summary>
    public Dictionary<string, TaskResult> Results { get; set; } =
        new Dictionary<string, TaskResult>(StringComparer.OrdinalIgnoreCase);

    /// <summary>运行记录创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>开始执行时间；尚未开始时为 null。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>结束时间；尚未结束时为 null。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
