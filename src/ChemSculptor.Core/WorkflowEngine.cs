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
        IReadOnlyList<string> violations = await _rules.ValidateWorkflowAsync(definition, cancellationToken);
        if (violations.Count > 0)
        {
            string message = string.Join("; ", violations);
            throw new InvalidOperationException("Workflow rejected by rule engine: " + message);
        }

        WorkflowRun run = new WorkflowRun();
        run.Id = definition.Id;
        run.Definition = definition;
        run.State = WorkflowState.Ready;
        run.NodeStates = new Dictionary<string, TaskState>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < definition.Nodes.Count; index++)
        {
            WorkflowNode node = definition.Nodes[index];
            run.NodeStates.Add(node.Id, TaskState.Pending);
        }

        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(WorkflowEventTypes.WorkflowStarted, run.Id, null, null, cancellationToken);
        return run;
    }

    public async Task<WorkflowRun> RunAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run = await _repository.GetAsync(workflowId, cancellationToken);
        if (run == null)
        {
            throw new KeyNotFoundException("Workflow '" + workflowId + "' was not found.");
        }

        if (!WorkflowStateRules.CanTransition(run.State, WorkflowState.Running))
        {
            return run;
        }

        run.State = WorkflowState.Running;
        run.StartedAt = DateTimeOffset.UtcNow;
        await _repository.SaveAsync(run, cancellationToken);

        Dictionary<string, WorkflowNode> nodes =
            new Dictionary<string, WorkflowNode>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < run.Definition.Nodes.Count; index++)
        {
            WorkflowNode node = run.Definition.Nodes[index];
            nodes.Add(node.Id, node);
        }

        Dictionary<string, TaskResult> completed =
            new Dictionary<string, TaskResult>(StringComparer.OrdinalIgnoreCase);

        HashSet<string> pending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string nodeId in nodes.Keys)
        {
            pending.Add(nodeId);
        }

        while (pending.Count > 0)
        {
            List<string> ready = new List<string>();

            foreach (string nodeId in pending)
            {
                WorkflowNode node = nodes[nodeId];
                bool dependenciesCompleted = true;

                for (int dependencyIndex = 0; dependencyIndex < node.DependsOn.Count; dependencyIndex++)
                {
                    string dependencyId = node.DependsOn[dependencyIndex];
                    if (!completed.ContainsKey(dependencyId))
                    {
                        dependenciesCompleted = false;
                        break;
                    }
                }

                if (dependenciesCompleted)
                {
                    ready.Add(nodeId);
                }
            }

            if (ready.Count == 0)
            {
                run.Results = completed;
                return await FailAsync(
                    run,
                    "Workflow DAG has a cycle or an unsatisfied dependency.",
                    cancellationToken);
            }

            for (int index = 0; index < ready.Count; index++)
            {
                string nodeId = ready[index];
                TaskResult result = await ExecuteNodeAsync(run, nodes[nodeId], completed, cancellationToken);
                completed[nodeId] = result;

                if (!result.Succeeded)
                {
                    run.Results = completed;
                    return await FailAsync(
                        run,
                        "Node '" + nodeId + "' failed: " + result.Diagnostics,
                        cancellationToken);
                }
            }

            for (int index = 0; index < ready.Count; index++)
            {
                pending.Remove(ready[index]);
            }
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
        ISkillContainer? container = _containers.Resolve(node.Container);
        if (container == null)
        {
            throw new InvalidOperationException("Skill container '" + node.Container + "' is not registered.");
        }

        run.NodeStates[node.Id] = TaskState.Running;
        await _repository.SaveAsync(run, cancellationToken);
        await EmitAsync(WorkflowEventTypes.TaskStarted, run.Id, node.Id, null, cancellationToken);

        TaskResult result;

        try
        {
            TaskRequest request = new TaskRequest();
            request.WorkflowId = run.Id;
            request.NodeId = node.Id;
            request.ContainerId = node.Container;
            request.Inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (KeyValuePair<string, TaskResult> pair in completed)
            {
                string? output = pair.Value.Output;
                if (output == null)
                {
                    output = string.Empty;
                }

                request.Inputs.Add(pair.Key, output);
            }

            result = await container.ExecuteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            result = new TaskResult();
            result.WorkflowId = run.Id;
            result.NodeId = node.Id;
            result.Succeeded = false;
            result.Diagnostics = ex.Message;
        }

        if (result.Succeeded && node.Gate != null)
        {
            ValidationReport report = await _validation.ValidateAsync(result, cancellationToken);
            if (report.Status == "Failed")
            {
                TaskResult failedResult = new TaskResult();
                failedResult.WorkflowId = result.WorkflowId;
                failedResult.NodeId = result.NodeId;
                failedResult.Succeeded = false;
                failedResult.Diagnostics = "Validation gate '" + node.Gate +
                    "' failed (confidence " + report.Confidence.ToString("P0") + ").";
                result = failedResult;
            }
        }

        if (result.Succeeded)
        {
            run.NodeStates[node.Id] = TaskState.Passed;
        }
        else
        {
            run.NodeStates[node.Id] = TaskState.Failed;
        }

        await _repository.SaveAsync(run, cancellationToken);

        string eventType = WorkflowEventTypes.TaskCompleted;
        if (!result.Succeeded)
        {
            eventType = WorkflowEventTypes.TaskFailed;
        }

        await EmitAsync(eventType, run.Id, node.Id, result.Diagnostics, cancellationToken);
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
        WorkflowEvent eventData = new WorkflowEvent();
        eventData.Type = type;
        eventData.WorkflowId = workflowId;
        eventData.NodeId = nodeId;
        eventData.Payload = payload;

        await _repository.AppendEventAsync(eventData, cancellationToken);
        await _events.PublishAsync(eventData, cancellationToken);
    }
}
