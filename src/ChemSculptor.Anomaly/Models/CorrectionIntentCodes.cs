namespace ChemSculptor.Anomaly.Models;

/// <summary>通用修正意图代码。</summary>
public static class CorrectionIntentCodes
{
    /// <summary>执行同自旋态的波函数优化。</summary>
    public const string WavefunctionOptimization =
        "wavefunction.optimize";

    /// <summary>修改体系自旋多重度。</summary>
    public const string ChangeSpinMultiplicity =
        "spin-multiplicity.change";

    /// <summary>增加几何优化最大循环次数。</summary>
    public const string IncreaseOptimizationMaxCycles =
        "optimization.max-cycles.increase";

    /// <summary>从检查点重启几何优化。</summary>
    public const string RestartOptimization =
        "optimization.restart";

    /// <summary>计算初始 Hessian。</summary>
    public const string UseOptimizationCalcFc =
        "optimization.initial-hessian.calc-fc";

    /// <summary>读取已有初始 Hessian。</summary>
    public const string UseOptimizationReadFc =
        "optimization.initial-hessian.read-fc";

    /// <summary>复用 SCF 修复方案。</summary>
    public const string ReuseScfRecovery =
        "scf.recovery.reuse";
}
