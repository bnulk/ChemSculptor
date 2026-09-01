namespace ChemSculptor.Domain;

public interface IRuleEngine
{
    Task<IReadOnlyList<string>> ValidateWorkflowAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default);
}

public interface IValidationGate
{
    Task<ValidationReport> ValidateAsync(TaskResult result, CancellationToken cancellationToken = default);
}

public sealed record ValidationReport
{
    public required string Status { get; init; }

    public required double Confidence { get; init; }

    public IReadOnlyList<string> Checks { get; init; } = [];
}

public interface ICaseMemory
{
    Task RecordAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

public interface ILlmGateway
{
    Task<string> SuggestWorkflowAsync(string goal, CancellationToken cancellationToken = default);
}
