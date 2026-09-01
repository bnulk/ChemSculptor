namespace ChemSculptor.InputProcessor;

public interface IClientInputParser
{
    Task<ProcessedClientRequest> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default);
}

public sealed record ProcessedClientRequest
{
    public required string WorkflowId { get; init; }

    public required string Goal { get; init; }

    public required string RawText { get; init; }

    public IReadOnlyList<string> Diagnostics { get; init; } = [];
}
