using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 内存版工作流仓储。
/// 适合开发和单机阶段，服务重启后数据会丢失。
/// </summary>
public sealed class InMemoryWorkflowRepository : IWorkflowRepository
{
    private readonly ConcurrentDictionary<string, WorkflowRun> _runs =
        new ConcurrentDictionary<string, WorkflowRun>(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, List<WorkflowEvent>> _logs =
        new ConcurrentDictionary<string, List<WorkflowEvent>>(StringComparer.OrdinalIgnoreCase);

    /// <summary>保存或覆盖一次运行记录。</summary>
    public Task SaveAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _runs[run.Id] = run;
        return Task.CompletedTask;
    }

    /// <summary>按标识查询运行记录；不存在时返回 null。</summary>
    public Task<WorkflowRun?> GetAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        WorkflowRun? run;
        if (_runs.TryGetValue(workflowId, out run))
        {
            return Task.FromResult<WorkflowRun?>(run);
        }

        return Task.FromResult<WorkflowRun?>(null);
    }

    /// <summary>列出所有运行记录。</summary>
    public IReadOnlyList<WorkflowRun> List()
    {
        List<WorkflowRun> runs = new List<WorkflowRun>();

        foreach (WorkflowRun run in _runs.Values)
        {
            runs.Add(run);
        }

        return runs;
    }

    /// <summary>追加一条事件日志。</summary>
    public Task AppendEventAsync(WorkflowEvent eventData, CancellationToken cancellationToken = default)
    {
        // GetOrAdd 保证同一个工作流只创建一个日志列表。
        List<WorkflowEvent> emptyLog = new List<WorkflowEvent>();
        List<WorkflowEvent> log = _logs.GetOrAdd(eventData.WorkflowId, emptyLog);

        // List<T> 不是线程安全的，必须针对当前日志列表加锁。
        lock (log)
        {
            log.Add(eventData);
        }

        return Task.CompletedTask;
    }

    /// <summary>读取指定工作流的事件日志副本。</summary>
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
