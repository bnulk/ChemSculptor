namespace ChemSculptor.Anomaly.Models;

/// <summary>波函数稳定性检查结果。</summary>
public enum WavefunctionStabilityStatus
{
    /// <summary>尚未执行或没有可用输出。</summary>
    NotPerformed,

    /// <summary>波函数稳定。</summary>
    Stable,

    /// <summary>波函数不稳定。</summary>
    Unstable,

    /// <summary>输出存在，但无法确定稳定性。</summary>
    Inconclusive
}
