using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Models;

/// <summary>创建派生恢复作业的请求。</summary>
public sealed class RecoveryJobPreparationRequest
{
    /// <summary>原始计算作业。</summary>
    public CalculationJob SourceJob { get; set; } = new CalculationJob();

    /// <summary>采用的修正计划。</summary>
    public CorrectionPlan Plan { get; set; } = new CorrectionPlan();
}
