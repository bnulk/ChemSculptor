namespace ChemSculptor.Domain;

public interface ISkillContainer
{
    string Name { get; }

    string Version { get; }

    IReadOnlyList<string> Capabilities { get; }

    Task<TaskResult> ExecuteAsync(TaskRequest request, CancellationToken cancellationToken = default);

    Task<bool> HealthAsync(CancellationToken cancellationToken = default);
}

public interface IContainerRegistry
{
    Task RegisterAsync(ISkillContainer container, CancellationToken cancellationToken = default);

    ISkillContainer? Resolve(string containerId);

    IReadOnlyList<ContainerDescriptor> List();
}

public interface IEventBus
{
    Task PublishAsync(WorkflowEvent @event, CancellationToken cancellationToken = default);

    IDisposable Subscribe(Func<WorkflowEvent, CancellationToken, Task> handler);
}

public interface IWorkflowRepository
{
    Task SaveAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    Task<WorkflowRun?> GetAsync(string workflowId, CancellationToken cancellationToken = default);

    IReadOnlyList<WorkflowRun> List();

    Task AppendEventAsync(WorkflowEvent @event, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(string workflowId, CancellationToken cancellationToken = default);
}
