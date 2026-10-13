namespace ChemSculptor.Anomaly.Models;

/// <summary>通用异常代码。</summary>
public static class CommonAnomalyCodes
{
    /// <summary>SCF 没有收敛。</summary>
    public const string ScfNotConverged = "scf.not_converged";

    /// <summary>SCF 达到最大迭代次数。</summary>
    public const string ScfMaxCycles = "scf.max_cycles";

    /// <summary>SCF 初始猜测失败。</summary>
    public const string ScfGuessFailed = "scf.guess_failed";

    /// <summary>输出文件缺失。</summary>
    public const string OutputMissing = "output.missing";

    /// <summary>缺少正常结束标志。</summary>
    public const string NormalTerminationMissing =
        "normal_termination.missing";

    /// <summary>缺少能量。</summary>
    public const string EnergyMissing = "energy.missing";

    /// <summary>波函数不稳定。</summary>
    public const string WavefunctionInstability = "wavefunction-instability";
}
