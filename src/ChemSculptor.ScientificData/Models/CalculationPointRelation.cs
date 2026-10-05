namespace ChemSculptor.ScientificData.Models;

/// <summary>两个计算点之间的科学关系。</summary>
public sealed class CalculationPointRelation
{
    /// <summary>关系标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>关系类别。</summary>
    public CalculationPointRelationKind Kind { get; set; } =
        CalculationPointRelationKind.DerivedFrom;

    /// <summary>起点计算点标识。</summary>
    public string FromPointId { get; set; } = string.Empty;

    /// <summary>终点计算点标识。</summary>
    public string ToPointId { get; set; } = string.Empty;

    /// <summary>关系显示名称。</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>在路径或序列中的顺序；不适用时为空。</summary>
    public int? Sequence { get; set; }

    /// <summary>关系说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
