namespace ChemSculptor.ScientificData.Models;

/// <summary>计算点对应的电子态。</summary>
public sealed class PointElectronicState
{
    /// <summary>电子态类别。</summary>
    public PointElectronicStateKind Kind { get; set; } =
        PointElectronicStateKind.Unknown;

    /// <summary>体系总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>体系自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>激发态或其它状态标签，例如 S1、T1。</summary>
    public string StateLabel { get; set; } = string.Empty;

    /// <summary>电子态说明。</summary>
    public string Description { get; set; } = string.Empty;
}
