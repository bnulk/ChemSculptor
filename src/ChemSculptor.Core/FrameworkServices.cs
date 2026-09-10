using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

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
