namespace ChemSculptor.ScientificData.Models;

/// <summary>计算点上保存的一项科学性质。</summary>
public sealed class ScientificProperty
{
    /// <summary>性质名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>性质类别。</summary>
    public ScientificPropertyKind Kind { get; set; } =
        ScientificPropertyKind.Other;

    /// <summary>数值内容；不适用时为空。</summary>
    public double? NumericValue { get; set; }

    /// <summary>文本或数组内容；不适用时为空。</summary>
    public string TextValue { get; set; } = string.Empty;

    /// <summary>单位。</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>数据来源说明。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
