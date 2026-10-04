using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>修正方案审批策略。</summary>
public interface IApprovalPolicy
{
    /// <summary>策略标识。</summary>
    string Name { get; }

    /// <summary>策略版本。</summary>
    string Version { get; }

    /// <summary>评价一个候选或最终修正方案。</summary>
    Task<ApprovalDecision> EvaluateAsync(
        AnomalyContext context,
        CorrectionOption option,
        CancellationToken cancellationToken = default);
}
