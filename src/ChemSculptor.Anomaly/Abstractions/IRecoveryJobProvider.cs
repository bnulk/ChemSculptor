using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>具体计算程序的派生恢复作业创建器。</summary>
public interface IRecoveryJobProvider
{
    /// <summary>适用的计算程序。</summary>
    string Program { get; }

    /// <summary>判断是否能够处理修正计划。</summary>
    bool CanPrepare(
        RecoveryJobPreparationRequest request);

    /// <summary>创建派生恢复作业。</summary>
    Task<RecoveryJobPreparationResult> PrepareAsync(
        RecoveryJobPreparationRequest request,
        CancellationToken cancellationToken = default);
}
