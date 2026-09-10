namespace ChemSculptor.Domain;

public sealed class TaskRequest
{
    public string WorkflowId { get; set; } = string.Empty;

    public string NodeId { get; set; } = string.Empty;

    public string ContainerId { get; set; } = string.Empty;

    public Dictionary<string, string> Inputs { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed class TaskResult
{
    public string WorkflowId { get; set; } = string.Empty;

    public string NodeId { get; set; } = string.Empty;

    public bool Succeeded { get; set; }

    public string? Output { get; set; }

    public string? Diagnostics { get; set; }

    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ContainerDescriptor
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public List<string> Capabilities { get; set; } = new List<string>();
}
