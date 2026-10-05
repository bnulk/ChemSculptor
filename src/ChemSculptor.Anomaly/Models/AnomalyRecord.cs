namespace ChemSculptor.Anomaly.Models;

/// <summary>
/// 一次异常处理的完整记录。
/// 该对象是异常处理存储的主要聚合根。
/// </summary>
public sealed class AnomalyRecord
{
    /// <summary>异常处理记录标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>关联的计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>关联的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>当前总体状态。</summary>
    public AnomalyRecordStatus Status { get; set; } =
        AnomalyRecordStatus.Open;

    /// <summary>异常评估结果。</summary>
    public AnomalyAssessment? Assessment { get; set; }

    /// <summary>执行过的异常检查。</summary>
    public List<AnomalyCheckResult> Checks { get; set; } =
        new List<AnomalyCheckResult>();

    /// <summary>诊断报告。</summary>
    public List<DiagnosisReport> Diagnoses { get; set; } =
        new List<DiagnosisReport>();

    /// <summary>修正计划。</summary>
    public List<CorrectionPlan> CorrectionPlans { get; set; } =
        new List<CorrectionPlan>();

    /// <summary>审批决定。</summary>
    public List<ApprovalDecision> ApprovalDecisions { get; set; } =
        new List<ApprovalDecision>();

    /// <summary>恢复尝试。</summary>
    public List<RecoveryAttempt> RecoveryAttempts { get; set; } =
        new List<RecoveryAttempt>();

    /// <summary>最终恢复结果。</summary>
    public RecoveryOutcome? Outcome { get; set; }

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>最后更新时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
