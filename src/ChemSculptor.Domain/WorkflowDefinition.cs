namespace ChemSculptor.Domain;

public sealed class WorkflowDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;

    public List<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
}

public sealed class WorkflowNode
{
    public string Id { get; set; } = string.Empty;

    public string Container { get; set; } = string.Empty;

    public List<string> DependsOn { get; set; } = new List<string>();

    public string? Gate { get; set; }
}
