namespace ChemSculptor.Anomaly.Models;

/// <summary>稳定性本征向量中的一个轨道跃迁分量。</summary>
public sealed class WavefunctionStabilityTransition
{
    /// <summary>起始轨道编号。</summary>
    public int FromOrbital { get; set; }

    /// <summary>目标轨道编号。</summary>
    public int ToOrbital { get; set; }

    /// <summary>跃迁系数。</summary>
    public double Coefficient { get; set; }
}
