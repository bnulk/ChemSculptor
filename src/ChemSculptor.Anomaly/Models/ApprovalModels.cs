namespace ChemSculptor.Anomaly.Models;

/// <summary>修正方案的审批状态。</summary>
public enum ApprovalStatus
{
    /// <summary>等待处理。</summary>
    Pending,

    /// <summary>由策略自动批准。</summary>
    AutoApproved,

    /// <summary>已由用户批准。</summary>
    Approved,

    /// <summary>已拒绝。</summary>
    Rejected,

    /// <summary>策略禁止执行。</summary>
    Forbidden
}

/// <summary>一条审批决定。</summary>
public sealed class ApprovalDecision
{
    /// <summary>审批决定标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>审批状态。</summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>审批原因。</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>决定者，例如策略名称或用户标识。</summary>
    public string DecidedBy { get; set; } = string.Empty;

    /// <summary>决定时间。</summary>
    public DateTimeOffset DecidedAt { get; set; } = DateTimeOffset.UtcNow;
}
