namespace ChemSculptor.Anomaly.Models;

/// <summary>波函数稳定性矩阵的一个本征向量。</summary>
public sealed class WavefunctionStabilityEigenvector
{
    /// <summary>本征向量编号。</summary>
    public int Index { get; set; }

    /// <summary>电子态名称，例如 Triplet-?Sym。</summary>
    public string StateName { get; set; } = string.Empty;

    /// <summary>由电子态名称推导出的自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>本征值。</summary>
    public double Eigenvalue { get; set; }

    /// <summary>该电子态的 S**2 期望值。</summary>
    public double SpinSquared { get; set; }

    /// <summary>轨道跃迁分量。</summary>
    public List<WavefunctionStabilityTransition> Transitions { get; set; } =
        new List<WavefunctionStabilityTransition>();
}
