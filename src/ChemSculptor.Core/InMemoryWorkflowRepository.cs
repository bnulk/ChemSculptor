using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, WorkflowRun> _runs =
        new ConcurrentDictionary<string, WorkflowRun>(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, List<WorkflowEvent>> _logs =
        new ConcurrentDictionary<string, List<WorkflowEvent>>(StringComparer.OrdinalIgnoreCase);

    public Task SaveAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    public Task<WorkflowRun?> GetAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run;
        if (_runs.TryGetValue(workflowId, out run))
        {
            return Task.FromResult<WorkflowRun?>(run);
        }

        return Task.FromResult<WorkflowRun?>(null);
    }

    public IReadOnlyList<WorkflowRun> List()
    {
        List<WorkflowRun> runs = new List<WorkflowRun>();

        foreach (WorkflowRun run in _runs.Values)
        {
            runs.Add(run);
        }

        return runs;
    }

    public Task AppendEventAsync(WorkflowEvent eventData, CancellationToken cancellationToken = default)
    {
        List<WorkflowEvent> emptyLog = new List<WorkflowEvent>();
        List<WorkflowEvent> log = _logs.GetOrAdd(eventData.WorkflowId, emptyLog);

        lock (log)
        {
            log.Add(eventData);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WorkflowEvent>> GetEventsAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        List<WorkflowEvent> events = new List<WorkflowEvent>();
        List<WorkflowEvent>? log;

        if (_logs.TryGetValue(workflowId, out log))
        {
            lock (log)
            {
                events.AddRange(log);
            }
        }

        return Task.FromResult<IReadOnlyList<WorkflowEvent>>(events);
    }
}
