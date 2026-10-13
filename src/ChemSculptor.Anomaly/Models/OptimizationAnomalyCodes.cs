namespace ChemSculptor.Anomaly.Models;

/// <summary>几何优化独有异常代码。</summary>
public static class OptimizationAnomalyCodes
{
    /// <summary>几何优化没有收敛。</summary>
    public const string NotConverged =
        "optimization.not_converged";

    /// <summary>几何优化达到最大循环次数。</summary>
    public const string MaxCyclesExceeded =
        "optimization.max_cycles_exceeded";

    /// <summary>力收敛指标未满足。</summary>
    public const string ForceNotConverged =
        "optimization.force_not_converged";

    /// <summary>位移收敛指标未满足。</summary>
    public const string DisplacementNotConverged =
        "optimization.displacement_not_converged";

    /// <summary>优化过程出现振荡。</summary>
    public const string Oscillation =
        "optimization.oscillation";

    /// <summary>优化步骤被拒绝。</summary>
    public const string StepRejected =
        "optimization.step_rejected";

    /// <summary>信赖半径失败。</summary>
    public const string TrustRadiusFailure =
        "optimization.trust_radius_failure";

    /// <summary>检查点文件缺失。</summary>
    public const string CheckpointMissing =
        "optimization.checkpoint_missing";

    /// <summary>优化几何无效。</summary>
    public const string GeometryInvalid =
        "optimization.geometry_invalid";
}
