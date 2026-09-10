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

public sealed class ValidationReport
{
    public string Status { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public List<string> Checks { get; set; } = new List<string>();
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
