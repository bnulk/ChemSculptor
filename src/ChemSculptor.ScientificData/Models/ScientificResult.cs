namespace ChemSculptor.ScientificData.Models;

/// <summary>
/// 一个科研工作流结束时保存的科学成果。
/// 成果由计算点集合、点之间的关系和导出物理量组成。
/// </summary>
public sealed class ScientificResult
{
    /// <summary>科学成果标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>科学成果标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>科学成果摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>科学成果状态。</summary>
    public ScientificResultStatus Status { get; set; } =
        ScientificResultStatus.Draft;

    /// <summary>本次成果使用的计算点集合。</summary>
    public CalculationPointSet PointSet { get; set; } =
        new CalculationPointSet();

    /// <summary>由计算点导出的物理量。</summary>
    public List<ScientificObservable> Observables { get; set; } =
        new List<ScientificObservable>();

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    /// <summary>完成时间。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
