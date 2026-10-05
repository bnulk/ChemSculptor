namespace ChemSculptor.ScientificData.Models;

/// <summary>一个科研工作使用的计算点集合。</summary>
public sealed class CalculationPointSet
{
    /// <summary>点集标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>点集名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>点集说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>包含的计算点。</summary>
    public List<CalculationPoint> Points { get; set; } =
        new List<CalculationPoint>();

    /// <summary>点之间的关系。</summary>
    public List<CalculationPointRelation> Relations { get; set; } =
        new List<CalculationPointRelation>();

    /// <summary>用于检索的标签。</summary>
    public List<string> Labels { get; set; } =
        new List<string>();

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
