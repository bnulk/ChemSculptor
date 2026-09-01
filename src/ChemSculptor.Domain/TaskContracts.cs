namespace ChemSculptor.Domain;

public sealed record TaskRequest
{
    public required string WorkflowId { get; init; }

    public required string NodeId { get; init; }

    public required string ContainerId { get; init; }

    public IReadOnlyDictionary<string, string> Inputs { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public sealed record TaskResult
{
    public required string WorkflowId { get; init; }

    public required string NodeId { get; init; }

    public required bool Succeeded { get; init; }

    public string? Output { get; init; }

    public string? Diagnostics { get; init; }

    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ContainerDescriptor
{
    public required string Id { get; init; }

    public required string Version { get; init; }

    public IReadOnlyList<string> Capabilities { get; init; } = [];
}
