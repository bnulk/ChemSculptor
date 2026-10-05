namespace ChemSculptor.Anomaly.Models;

/// <summary>一次异常检查的执行记录。</summary>
public sealed class AnomalyCheckResult
{
    /// <summary>检查代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>产生本次结果的具体实现标识。</summary>
    public string ImplementationId { get; set; } = string.Empty;

    /// <summary>检查显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>检查分类。</summary>
    public AnomalyCategory Category { get; set; } = AnomalyCategory.Unknown;

    /// <summary>检查获取证据的方式。</summary>
    public AnomalyCheckMechanism Mechanism { get; set; } =
        AnomalyCheckMechanism.Unknown;

    /// <summary>检查状态。</summary>
    public AnomalyCheckStatus Status { get; set; } = AnomalyCheckStatus.NotRun;

    /// <summary>检查是否属于任务必需项。</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>检查摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>跳过的原因。</summary>
    public string SkippedReason { get; set; } = string.Empty;

    /// <summary>需要辅助计算时关联的作业标识。</summary>
    public string AuxiliaryJobId { get; set; } = string.Empty;

    /// <summary>检查发现的所有异常。</summary>
    public List<AnomalyFinding> Findings { get; set; } =
        new List<AnomalyFinding>();

    /// <summary>检查证据。</summary>
    public List<AnomalyEvidence> Evidence { get; set; } =
        new List<AnomalyEvidence>();

    /// <summary>开始时间。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
