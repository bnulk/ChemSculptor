using System.Collections.Concurrent;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class AllowAllRuleEngine : IRuleEngine
{
    public Task<IReadOnlyList<string>> ValidateWorkflowAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}

public sealed class PassThroughValidationGate : IValidationGate
{
    public Task<ValidationReport> ValidateAsync(
        TaskResult result,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ValidationReport
        {
            Status = "Passed",
            Confidence = 1.0,
            Checks = ["framework placeholder gate"]
        });
}

public sealed class InMemoryCaseMemory : ICaseMemory
{
    private readonly ConcurrentQueue<WorkflowRun> _cases = [];

    public Task RecordAsync(WorkflowRun run, CancellationToken cancellationToken = default)
    {
        _cases.Enqueue(run);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>([]);
}
