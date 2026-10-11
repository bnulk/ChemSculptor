namespace ChemSculptor.Compute;

/// <summary>计算参数值类型。</summary>
public enum CalculationParameterValueKind
{
    /// <summary>字符串。</summary>
    String,

    /// <summary>整数。</summary>
    Integer,

    /// <summary>浮点数。</summary>
    Double,

    /// <summary>布尔值。</summary>
    Boolean,

    /// <summary>枚举值。</summary>
    Enum,

    /// <summary>字符串列表。</summary>
    StringList
}

/// <summary>规范计算参数定义。</summary>
public sealed class CalculationParameterDefinition
{
    /// <summary>规范参数键。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>参数值类型。</summary>
    public CalculationParameterValueKind ValueKind { get; set; }

    /// <summary>允许的规范值。</summary>
    public List<string> AllowedValues { get; set; } = new List<string>();

    /// <summary>全局默认值。</summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>风险等级。</summary>
    public CalculationRiskLevel RiskLevel { get; set; }

    /// <summary>是否需要人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>可识别的输入别名。</summary>
    public List<string> Aliases { get; set; } = new List<string>();

    /// <summary>适用的任务类型。</summary>
    public List<CalculationTaskType> TaskTypes { get; set; } =
        new List<CalculationTaskType>();

    /// <summary>适用的计算程序。</summary>
    public List<string> Programs { get; set; } = new List<string>();
}

/// <summary>解析完成的计算参数。</summary>
public sealed class ResolvedCalculationParameter
{
    /// <summary>规范参数键。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>解析后的规范值。</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>用户原始输入片段。</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>参数值来源。</summary>
    public ParameterSource Source { get; set; }

    /// <summary>解析置信度。</summary>
    public double Confidence { get; set; }

    /// <summary>风险等级。</summary>
    public CalculationRiskLevel RiskLevel { get; set; }

    /// <summary>是否需要人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>翻译后的程序关键词。</summary>
    public string TranslatedKeyword { get; set; } = string.Empty;
}
