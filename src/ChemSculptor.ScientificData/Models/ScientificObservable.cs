namespace ChemSculptor.ScientificData.Models;

/// <summary>由计算点集合导出的物理量。</summary>
public sealed class ScientificObservable
{
    /// <summary>物理量标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>物理量名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>物理量类别。</summary>
    public ScientificObservableKind Kind { get; set; } =
        ScientificObservableKind.Other;

    /// <summary>数值结果；不适用时为空。</summary>
    public double? NumericValue { get; set; }

    /// <summary>文本或结构化结果；不适用时为空。</summary>
    public string TextValue { get; set; } = string.Empty;

    /// <summary>单位。</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>导出该物理量所使用的公式或方法。</summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>生成该物理量所依赖的计算点标识。</summary>
    public List<string> PointIds { get; set; } =
        new List<string>();

    /// <summary>生成该物理量所依赖的关系标识。</summary>
    public List<string> RelationIds { get; set; } =
        new List<string>();

    /// <summary>结果摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
