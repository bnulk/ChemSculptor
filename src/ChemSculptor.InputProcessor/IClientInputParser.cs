namespace ChemSculptor.InputProcessor;

public interface IClientInputParser
{
    Task<ProcessedClientRequest> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default);
}

public sealed class ProcessedClientRequest
{
    public string WorkflowId { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public List<string> Diagnostics { get; set; } = new List<string>();
}
