namespace ChemSculptor.Anomaly.Models;

/// <summary>描述一种可注册的异常。</summary>
public sealed class AnomalyDescriptor
{
    /// <summary>异常代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>异常分类。</summary>
    public AnomalyCategory Category { get; set; } = AnomalyCategory.Unknown;

    /// <summary>异常处理模块版本。</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>异常说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否需要科学判断。</summary>
    public bool RequiresScientificJudgment { get; set; }
}
