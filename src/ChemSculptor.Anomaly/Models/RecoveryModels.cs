namespace ChemSculptor.Anomaly.Models;

/// <summary>一次派生恢复作业。</summary>
public sealed class RecoveryAttempt
{
    /// <summary>本次恢复尝试标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>父作业标识。</summary>
    public string ParentJobId { get; set; } = string.Empty;

    /// <summary>派生作业标识。</summary>
    public string RecoveryJobId { get; set; } = string.Empty;

    /// <summary>第几次恢复尝试。</summary>
    public int AttemptNumber { get; set; }

    /// <summary>采用的修正计划标识。</summary>
    public string CorrectionPlanId { get; set; } = string.Empty;

    /// <summary>审批决定标识。</summary>
    public string ApprovalDecisionId { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>恢复验证结果。</summary>
public sealed class RecoveryValidationResult
{
    /// <summary>派生作业是否验证成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>是否解决了原始异常。</summary>
    public bool ResolvedOriginalAnomaly { get; set; }

    /// <summary>验证摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>补充信息。</summary>
    public List<string> Details { get; set; } = new List<string>();
}

/// <summary>异常处理的最终结果。</summary>
public enum RecoveryOutcomeStatus
{
    /// <summary>已恢复。</summary>
    Recovered,

    /// <summary>需要用户决定。</summary>
    NeedsUserDecision,

    /// <summary>超出预算。</summary>
    BudgetExceeded,

    /// <summary>没有安全修正方案。</summary>
    NoSafeCorrection,

    /// <summary>处理失败。</summary>
    Failed,

    /// <summary>已终止。</summary>
    Aborted
}

/// <summary>异常处理的最终结果。</summary>
public sealed class RecoveryOutcome
{
    /// <summary>结果状态。</summary>
    public RecoveryOutcomeStatus Status { get; set; } =
        RecoveryOutcomeStatus.Aborted;

    /// <summary>结果摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>可能使用的最终恢复作业。</summary>
    public string? FinalRecoveryJobId { get; set; }

    /// <summary>补充说明。</summary>
    public List<string> Details { get; set; } = new List<string>();
}
