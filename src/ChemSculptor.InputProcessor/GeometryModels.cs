namespace ChemSculptor.InputProcessor;

/// <summary>
/// 单个原子的元素符号与三维坐标。
/// </summary>
public sealed class GeometryAtom
{
    /// <summary>元素符号，例如 O、H、C。</summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>X 坐标。</summary>
    public double X { get; set; }

    /// <summary>Y 坐标。</summary>
    public double Y { get; set; }

    /// <summary>Z 坐标。</summary>
    public double Z { get; set; }
}

/// <summary>
/// 一个分子的几何解析结果。
/// </summary>
public sealed class MolecularGeometry
{
    /// <summary>分子名称；输入未提供名称时使用默认值。</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>根据原子列表统计出的分子式，例如 H2O。</summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>原始坐标文本，便于溯源。</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>解析出的原子列表。</summary>
    public List<GeometryAtom> Atoms { get; set; } = new List<GeometryAtom>();

    /// <summary>解析过程中的诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}
