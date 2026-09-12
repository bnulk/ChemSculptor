using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 占位规则引擎：不进行任何校验，全部放行。
/// 真实规则实现后应替换本类。
/// </summary>
public sealed class AllowAllRuleEngine : IRuleEngine
{
    public Task<IReadOnlyList<string>> ValidateWorkflowAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> violations = new List<string>();
        return Task.FromResult(violations);
    }
}

/// <summary>
/// 占位验证门：所有结果都直接通过。
/// 真实验证逻辑实现后应替换本类。
/// </summary>
public sealed class PassThroughValidationGate : IValidationGate
{
    public Task<ValidationReport> ValidateAsync(
        TaskResult result,
        CancellationToken cancellationToken = default)
    {
        ValidationReport report = new ValidationReport();
        report.Status = "Passed";
        report.Confidence = 1.0;
        report.Checks = new List<string>();
        report.Checks.Add("framework placeholder gate");

        return Task.FromResult(report);
    }
}

/// <summary>
/// 内存版案例记忆：只记录运行结果，暂不提供检索。
/// </summary>
public sealed class InMemoryCaseMemory : ICaseMemory
{
    private readonly ConcurrentQueue<WorkflowRun> _cases = new ConcurrentQueue<WorkflowRun>();

    public Task RecordAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _cases.Enqueue(run);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> results = new List<string>();
        return Task.FromResult(results);
    }
}
