namespace ChemSculptor.Anomaly.Models;

/// <summary>异常处理通用 Skill 标识。</summary>
public static class AnomalySkillIds
{
    /// <summary>检查波函数稳定性。</summary>
    public const string CheckWavefunctionStability =
        "anomaly.check-wavefunction-stability";

    /// <summary>生成波函数稳定性修正方案。</summary>
    public const string PlanWavefunctionStabilityCorrection =
        "anomaly.plan-wavefunction-stability-correction";

    /// <summary>创建派生恢复作业。</summary>
    public const string CreateRecoveryJob =
        "anomaly.create-recovery-job";

    /// <summary>执行派生恢复作业。</summary>
    public const string ExecuteRecoveryJob =
        "anomaly.execute-recovery-job";

    /// <summary>诊断优化异常。</summary>
    public const string DiagnoseOptimizationFailure =
        "anomaly.diagnose-optimization-failure";

    /// <summary>规划优化修正方案。</summary>
    public const string PlanOptimizationCorrection =
        "anomaly.plan-optimization-correction";

    /// <summary>创建优化派生恢复作业。</summary>
    public const string CreateOptimizationRecoveryJob =
        "anomaly.create-optimization-recovery-job";

    /// <summary>执行优化派生恢复作业。</summary>
    public const string ExecuteOptimizationRecoveryJob =
        "anomaly.execute-optimization-recovery-job";

    /// <summary>验证优化恢复结果。</summary>
    public const string ValidateOptimizationRecovery =
        "anomaly.validate-optimization-recovery";
}
