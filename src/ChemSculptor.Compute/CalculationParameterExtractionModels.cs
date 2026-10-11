namespace ChemSculptor.Compute;

/// <summary>参数意图状态。</summary>
public enum CalculationParameterIntentState
{
    /// <summary>已提取到明确值。</summary>
    ExplicitValue,

    /// <summary>只识别到参数键，缺少值。</summary>
    KeyOnly,

    /// <summary>存在多种解释。</summary>
    Ambiguous,

    /// <summary>同一条文本中存在冲突。</summary>
    Conflicting,

    /// <summary>当前阶段不支持。</summary>
    Unsupported
}

/// <summary>对话参数提取上下文。</summary>
public sealed class CalculationParameterExtractionContext
{
    /// <summary>目标计算引擎。</summary>
    public string Engine { get; set; } =
        CalculationDefaults.DefaultProgram;

    /// <summary>目标任务类型。</summary>
    public CalculationTaskType TaskType { get; set; } =
        CalculationTaskType.SinglePoint;
}

/// <summary>从对话文本提取出的参数意图。</summary>
public sealed class CalculationParameterIntent
{
    /// <summary>规范参数键。</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>候选规范值。</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>用户原始文本片段。</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>意图状态。</summary>
    public CalculationParameterIntentState State { get; set; }

    /// <summary>参数来源。</summary>
    public ParameterSource Source { get; set; } = ParameterSource.User;

    /// <summary>提取置信度。</summary>
    public double Confidence { get; set; }

    /// <summary>原文起始位置。</summary>
    public int StartIndex { get; set; }

    /// <summary>原文结束位置，不包含该位置。</summary>
    public int EndIndex { get; set; }
}

/// <summary>对话参数提取结果。</summary>
public sealed class CalculationParameterExtractionResult
{
    /// <summary>提取出的参数意图。</summary>
    public List<CalculationParameterIntent> Intents { get; set; } =
        new List<CalculationParameterIntent>();

    /// <summary>提取诊断信息。</summary>
    public List<string> Diagnostics { get; set; } =
        new List<string>();
}
