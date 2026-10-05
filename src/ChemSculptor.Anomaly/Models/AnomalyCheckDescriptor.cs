namespace ChemSculptor.Anomaly.Models;

/// <summary>描述一项异常检查。</summary>
public sealed class AnomalyCheckDescriptor
{
    /// <summary>检查代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>异常分类。</summary>
    public AnomalyCategory Category { get; set; } = AnomalyCategory.Unknown;

    /// <summary>检查模块版本。</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>该项检查是否属于任务要求的一部分。</summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>检查说明。</summary>
    public string Description { get; set; } = string.Empty;
}
