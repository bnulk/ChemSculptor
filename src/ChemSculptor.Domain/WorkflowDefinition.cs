namespace ChemSculptor.Domain;

public sealed record WorkflowDefinition
{
    public required string Id { get; init; }

    public required string Version { get; init; }

    public required string Goal { get; init; }

    public IReadOnlyList<WorkflowNode> Nodes { get; init; } = [];
}

public sealed record WorkflowNode
{
    public required string Id { get; init; }

    public required string Container { get; init; }

    public IReadOnlyList<string> DependsOn { get; init; } = [];

    public string? Gate { get; init; }
}
