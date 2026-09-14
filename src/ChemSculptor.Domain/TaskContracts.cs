namespace ChemSculptor.Domain;

/// <summary>
/// 内核发给技能容器的任务单。
/// </summary>
public sealed class TaskRequest
{
    /// <summary>任务所属的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>任务对应的节点标识。</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>要调用的技能标识。</summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>上游节点的输出，键为上游节点标识。</summary>
    public Dictionary<string, string> Inputs { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// 技能容器返回给内核的执行回执。
/// </summary>
public sealed class TaskResult
{
    /// <summary>回执所属的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>回执对应的节点标识。</summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>是否执行成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>成功时的输出内容。</summary>
    public string? Output { get; set; }

    /// <summary>失败时的诊断信息。</summary>
    public string? Diagnostics { get; set; }

    /// <summary>完成时间。</summary>
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 技能对外展示的元信息。
/// 它只包含名称、版本和能力标签，不包含可执行逻辑。
/// </summary>
public sealed class SkillDescriptor
{
    /// <summary>技能唯一标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>技能版本号。</summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>技能能力标签。</summary>
    public List<string> Capabilities { get; set; } = new List<string>();
}
