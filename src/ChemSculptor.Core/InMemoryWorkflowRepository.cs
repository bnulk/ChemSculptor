using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, WorkflowRun> _runs =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, List<WorkflowEvent>> _logs =
        new(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    public Task<WorkflowRun?> GetAsync(string workflowId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_runs.TryGetValue(workflowId, out var run) ? run : null);

    public IReadOnlyList<WorkflowRun> List() => _runs.Values.ToList();

    public Task AppendEventAsync(WorkflowEvent @event, CancellationToken cancellationToken = default)
    {
        var log = _logs.GetOrAdd(@event.WorkflowId, static _ => []);
        lock (log)
        {
            log.Add(@event);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WorkflowEvent> events = _logs.TryGetValue(workflowId, out var log)
            ? [.. log]
            : [];

        return Task.FromResult(events);
    }
}
