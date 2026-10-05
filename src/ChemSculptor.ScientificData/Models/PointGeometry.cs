namespace ChemSculptor.ScientificData.Models;

/// <summary>一个计算点使用的几何结构。</summary>
public sealed class PointGeometry
{
    /// <summary>几何结构标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>原始几何资产标识；没有外部来源时为空。</summary>
    public string SourceGeometryId { get; set; } = string.Empty;

    /// <summary>规范化坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>规范化几何结构的摘要值，用于比较和去重。</summary>
    public string CanonicalHash { get; set; } = string.Empty;

    /// <summary>按原子顺序保存的坐标。</summary>
    public List<PointAtom> Atoms { get; set; } =
        new List<PointAtom>();
}

/// <summary>几何结构中的一个原子位置。</summary>
public sealed class PointAtom
{
    /// <summary>点在原子序列中的序号。</summary>
    public int Index { get; set; }

    /// <summary>元素符号。</summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>原子序数。</summary>
    public int AtomicNumber { get; set; }

    /// <summary>X 坐标。</summary>
    public double X { get; set; }

    /// <summary>Y 坐标。</summary>
    public double Y { get; set; }

    /// <summary>Z 坐标。</summary>
    public double Z { get; set; }
}

/// <summary>组合体系在某个计算点中的组成信息。</summary>
public sealed class PointComponent
{
    /// <summary>组分标识。</summary>
    public string ComponentId { get; set; } = string.Empty;

    /// <summary>组分显示名称。</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>组分中的原子序号。</summary>
    public List<int> AtomIndices { get; set; } =
        new List<int>();

    /// <summary>组分电荷。</summary>
    public int Charge { get; set; }

    /// <summary>组分自旋多重度。</summary>
    public int Multiplicity { get; set; }
}
