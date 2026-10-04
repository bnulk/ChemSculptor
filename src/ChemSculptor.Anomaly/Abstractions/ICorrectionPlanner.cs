using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>修正方案规划器。</summary>
public interface ICorrectionPlanner
{
    /// <summary>能够处理的异常代码。</summary>
    string Code { get; }

    /// <summary>规划器版本。</summary>
    string Version { get; }

    /// <summary>判断是否能够为诊断结果提出修正方案。</summary>
    bool CanPlan(DiagnosisReport diagnosis);

    /// <summary>提出候选修正方案。</summary>
    Task<IReadOnlyList<CorrectionOption>> PlanAsync(
        AnomalyContext context,
        DiagnosisReport diagnosis,
        CancellationToken cancellationToken = default);
}
