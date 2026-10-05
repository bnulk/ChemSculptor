namespace ChemSculptor.ScientificData.Models;

/// <summary>
/// 一个计算点。
/// 计算点表示确定几何、电子态和计算模型下的一个科学数据单位。
/// </summary>
public sealed class CalculationPoint
{
    /// <summary>计算点标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>计算点显示名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>计算点类别。</summary>
    public CalculationPointKind Kind { get; set; } =
        CalculationPointKind.Unknown;

    /// <summary>计算点在科学数据中的接受状态。</summary>
    public CalculationPointStatus Status { get; set; } =
        CalculationPointStatus.Candidate;

    /// <summary>几何结构。</summary>
    public PointGeometry Geometry { get; set; } =
        new PointGeometry();

    /// <summary>组合体系中的组分信息。</summary>
    public List<PointComponent> Components { get; set; } =
        new List<PointComponent>();

    /// <summary>电子态。</summary>
    public PointElectronicState ElectronicState { get; set; } =
        new PointElectronicState();

    /// <summary>计算条件。</summary>
    public PointCalculationModel CalculationModel { get; set; } =
        new PointCalculationModel();

    /// <summary>计算点上保存的性质。</summary>
    public List<ScientificProperty> Properties { get; set; } =
        new List<ScientificProperty>();

    /// <summary>计算点上执行过的验证。</summary>
    public List<PointValidationRecord> Validations { get; set; } =
        new List<PointValidationRecord>();

    /// <summary>来源和接受过程。</summary>
    public PointProvenance Provenance { get; set; } =
        new PointProvenance();

    /// <summary>用于检索的标签。</summary>
    public List<string> Labels { get; set; } =
        new List<string>();

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } =
        DateTimeOffset.UtcNow;

    /// <summary>最后更新时间。</summary>
    public DateTimeOffset UpdatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
