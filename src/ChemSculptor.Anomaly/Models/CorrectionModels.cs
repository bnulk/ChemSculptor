namespace ChemSculptor.Anomaly.Models;

/// <summary>修正动作的风险等级。</summary>
public enum CorrectionRiskLevel
{
    /// <summary>低风险，通常可逆。</summary>
    Low,

    /// <summary>中等风险。</summary>
    Medium,

    /// <summary>高风险，可能改变科学含义。</summary>
    High,

    /// <summary>禁止自动执行。</summary>
    Forbidden
}

/// <summary>一个具体的计划修改项。</summary>
public sealed class CorrectionChange
{
    /// <summary>被修改的参数名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>修改前的值。</summary>
    public string OldValue { get; set; } = string.Empty;

    /// <summary>修改后的值。</summary>
    public string NewValue { get; set; } = string.Empty;

    /// <summary>修改原因。</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>一个候选修正方案。</summary>
public sealed class CorrectionOption
{
    /// <summary>候选方案标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>通用修正意图代码。</summary>
    public string IntentCode { get; set; } = string.Empty;

    /// <summary>方案标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>方案说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>风险等级。</summary>
    public CorrectionRiskLevel RiskLevel { get; set; } =
        CorrectionRiskLevel.High;

    /// <summary>是否需要人工审批。</summary>
    public bool RequiresApproval { get; set; } = true;

    /// <summary>是否可以用于创建恢复作业。</summary>
    public bool CanUseForRestart { get; set; }

    /// <summary>具体修改项。</summary>
    public List<CorrectionChange> Changes { get; set; } =
        new List<CorrectionChange>();
}

/// <summary>最终采用的修正计划。</summary>
public sealed class CorrectionPlan
{
    /// <summary>计划标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>原始作业标识。</summary>
    public string SourceJobId { get; set; } = string.Empty;

    /// <summary>最终采用的候选方案。</summary>
    public CorrectionOption? Option { get; set; }

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
