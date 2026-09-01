using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class WorkflowEngine
{
    private readonly IContainerRegistry _containers;
    private readonly IEventBus _events;
    private readonly IWorkflowRepository _repository;
    private readonly IRuleEngine _rules;
    private readonly IValidationGate _validation;
    private readonly ICaseMemory _memory;

    public WorkflowEngine(
        IContainerRegistry containers,
        IEventBus events,
        IWorkflowRepository repository,
        IRuleEngine rules,
        IValidationGate validation,
        ICaseMemory memory)
    {
        _containers = containers;
        _events = events;
        _repository = repository;
        _rules = rules;
        _validation = validation;
        _memory = memory;
    }

    public async Task<WorkflowRun> SubmitAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        var violations = await _rules.ValidateWorkflowAsync(definition, cancellationToken);
        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                $"Workflow rejected by rule engine: {string.Join("; ", violations)}");
        }

        var run = new WorkflowRun
        {
            Id = definition.Id,
            Definition = definition,
            State = WorkflowState.Ready,
            NodeStates = definition.Nodes.ToDictionary(
                node => node.Id,
                _ => TaskState.Pending,
                StringComparer.OrdinalIgnoreCase)
        };

        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(WorkflowEventTypes.WorkflowStarted, run.Id, null, null, cancellationToken);
        return run;
    }

    public async Task<WorkflowRun> RunAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var run = await _repository.GetAsync(workflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow '{workflowId}' was not found.");

        if (!WorkflowStateRules.CanTransition(run.State, WorkflowState.Running))
        {
            return run;
        }

        run.State = WorkflowState.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(run, cancellationToken);

        var nodes = run.Definition.Nodes.ToDictionary(
            node => node.Id,
            StringComparer.OrdinalIgnoreCase);
        var completed = new Dictionary<string, TaskResult>(StringComparer.OrdinalIgnoreCase);
        var pending = new HashSet<string>(nodes.Keys, StringComparer.OrdinalIgnoreCase);

        while (pending.Count > 0)
        {
            var ready = pending
                .Where(id => nodes[id].DependsOn.All(completed.ContainsKey))
                .ToList();

            if (ready.Count == 0)
            {
                run.Results = completed;
                return await FailAsync(
                    run,
                    "Workflow DAG has a cycle or an unsatisfied dependency.",
                    cancellationToken);
            }

            foreach (var nodeId in ready)
            {
                var result = await ExecuteNodeAsync(run, nodes[nodeId], completed, cancellationToken);
                completed[nodeId] = result;

                if (!result.Succeeded)
                {
                    run.Results = completed;
                    return await FailAsync(
                        run,
                        $"Node '{nodeId}' failed: {result.Diagnostics}",
                        cancellationToken);
                }
            }

            pending.ExceptWith(ready);
        }

        run.State = WorkflowState.Passed;
        run.Results = completed;
        run.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(WorkflowEventTypes.WorkflowCompleted, run.Id, null, null, cancellationToken);
        await _memory.RecordAsync(run, cancellationToken);
        return run;
    }

    private async Task<TaskResult> ExecuteNodeAsync(
        WorkflowRun run,
        WorkflowNode node,
        IReadOnlyDictionary<string, TaskResult> completed,
        CancellationToken cancellationToken)
    {
        var container = _containers.Resolve(node.Container)
            ?? throw new InvalidOperationException($"Skill container '{node.Container}' is not registered.");

        run.NodeStates[node.Id] = TaskState.Running;
        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(WorkflowEventTypes.TaskStarted, run.Id, node.Id, null, cancellationToken);

        TaskResult result;
        try
        {
            var request = new TaskRequest
            {
                WorkflowId = run.Id,
                NodeId = node.Id,
                ContainerId = node.Container,
                Inputs = completed.ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value.Output ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
            };

            result = await container.ExecuteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            result = new TaskResult
            {
                WorkflowId = run.Id,
                NodeId = node.Id,
                Succeeded = false,
                Diagnostics = ex.Message
            };
        }

        if (result.Succeeded && node.Gate is not null)
        {
            var report = await _validation.ValidateAsync(result, cancellationToken);
            if (report.Status == "Failed")
            {
                result = new TaskResult
                {
                    WorkflowId = result.WorkflowId,
                    NodeId = result.NodeId,
                    Succeeded = false,
                    Diagnostics =
                        $"Validation gate '{node.Gate}' failed (confidence {report.Confidence:P0})."
                };
            }
        }

        run.NodeStates[node.Id] = result.Succeeded ? TaskState.Passed : TaskState.Failed;
        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(
            result.Succeeded ? WorkflowEventTypes.TaskCompleted : WorkflowEventTypes.TaskFailed,
            run.Id,
            node.Id,
            result.Diagnostics,
            cancellationToken);

        return result;
    }

    private async Task<WorkflowRun> FailAsync(
        WorkflowRun run,
        string reason,
        CancellationToken cancellationToken)
    {
        run.State = WorkflowState.Failed;
        run.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(WorkflowEventTypes.WorkflowFailed, run.Id, null, reason, cancellationToken);
        await _memory.RecordAsync(run, cancellationToken);
        return run;
    }

    private async Task EmitAsync(
        string type,
        string workflowId,
        string? nodeId,
        string? payload,
        CancellationToken cancellationToken)
    {
        var @event = new WorkflowEvent
        {
            Type = type,
            WorkflowId = workflowId,
            NodeId = nodeId,
            Payload = payload
        };

        await _repository.AppendEventAsync(@event, cancellationToken);
        await _events.PublishAsync(@event, cancellationToken);
    }
}
