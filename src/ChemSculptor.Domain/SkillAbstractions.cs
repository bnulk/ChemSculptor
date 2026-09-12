namespace ChemSculptor.Domain;

/// <summary>
/// 技能容器统一契约。
/// 内核只依赖该接口，不依赖任何具体化学软件实现。
/// </summary>
public interface ISkillContainer
{
    /// <summary>容器唯一名称。</summary>
    string Name { get; }

    /// <summary>容器版本。</summary>
    string Version { get; }

    /// <summary>容器能力标签。</summary>
    IReadOnlyList<string> Capabilities { get; }

    /// <summary>执行一次任务并返回结果。</summary>
    Task<TaskResult> ExecuteAsync(TaskRequest request, CancellationToken cancellationToken = default);

    /// <summary>检查容器当前是否可用。</summary>
    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 技能容器注册表。
/// </summary>
public interface IContainerRegistry
{
    /// <summary>注册一个技能容器。</summary>
    Task RegisterAsync(ISkillContainer container, CancellationToken cancellationToken = default);

    /// <summary>按名称查找技能容器；找不到时返回 null。</summary>
    ISkillContainer? Resolve(string containerId);

    /// <summary>列出所有已注册容器的元信息。</summary>
    IReadOnlyList<ContainerDescriptor> List();
}

/// <summary>
/// 事件总线。
/// 内核通过它发布事件，订阅者自行决定如何处理。
/// </summary>
public interface IEventBus
{
    /// <summary>发布一条事件。</summary>
    Task PublishAsync(WorkflowEvent @event, CancellationToken cancellationToken = default);

    /// <summary>订阅事件；返回的对象可用于取消订阅。</summary>
    IDisposable Subscribe(Func<WorkflowEvent, CancellationToken, Task> handler);
}

/// <summary>
/// 工作流运行记录与事件日志的存储抽象。
/// </summary>
public interface IWorkflowRepository
{
    /// <summary>保存或更新一次运行记录。</summary>
    Task SaveAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    /// <summary>按标识查询运行记录；不存在时返回 null。</summary>
    Task<WorkflowRun?> GetAsync(string workflowId, CancellationToken cancellationToken = default);

    /// <summary>列出所有运行记录。</summary>
    IReadOnlyList<WorkflowRun> List();

    /// <summary>追加一条事件日志。</summary>
    Task AppendEventAsync(WorkflowEvent @event, CancellationToken cancellationToken = default);

    /// <summary>读取指定工作流的事件日志。</summary>
    Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(string workflowId, CancellationToken cancellationToken = default);
}
