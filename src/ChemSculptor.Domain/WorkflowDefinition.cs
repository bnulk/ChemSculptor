namespace ChemSculptor.Domain;

/// <summary>
/// 描述一份声明式工作流模板。
/// 模板只描述“要做什么、依赖关系是什么”，不包含运行状态。
/// </summary>
public sealed class WorkflowDefinition
{
    /// <summary>工作流唯一标识，用于查询、日志和执行记录关联。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>模板版本号，便于以后区分不同版本的工作流定义。</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>工作流的科研目标描述。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>工作流中的所有节点。</summary>
    public List<WorkflowNode> Nodes { get; set; } = new List<WorkflowNode>();
}

/// <summary>
/// 工作流中的一个执行节点。
/// 节点声明使用哪个技能容器、依赖哪些前置节点，以及是否需要验证门。
/// </summary>
public sealed class WorkflowNode
{
    /// <summary>节点唯一标识，同一个工作流内不允许重复。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>要调用的技能容器名称。</summary>
    public string Container { get; set; } = string.Empty;

    /// <summary>前置节点标识列表，所有前置节点完成后本节点才可执行。</summary>
    public List<string> DependsOn { get; set; } = new List<string>();

    /// <summary>可选的验证门名称；为空表示本节点不需要额外验证。</summary>
    public string? Gate { get; set; }
}
