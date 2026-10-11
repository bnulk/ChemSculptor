namespace ChemSculptor.Compute;

/// <summary>参数解析状态。</summary>
public enum CalculationParameterResolutionStatus
{
    /// <summary>参数已解析完成。</summary>
    Resolved,

    /// <summary>需要用户补充或确认参数。</summary>
    NeedsClarification,

    /// <summary>参数被拒绝。</summary>
    Rejected
}

/// <summary>参数问题类型。</summary>
public enum CalculationParameterQuestionType
{
    /// <summary>从有限选项中选择。</summary>
    Choice,

    /// <summary>提供一个值。</summary>
    Value
}

/// <summary>参数解析上下文。</summary>
public sealed class CalculationParameterResolutionContext
{
    /// <summary>目标计算引擎。</summary>
    public string Engine { get; set; } =
        CalculationDefaults.DefaultProgram;

    /// <summary>目标任务类型。</summary>
    public CalculationTaskType TaskType { get; set; } =
        CalculationTaskType.SinglePoint;

    /// <summary>已经应用电荷后的总电子数。</summary>
    public int? ElectronCount { get; set; }
}

/// <summary>参数解析问题。</summary>
public sealed class CalculationParameterQuestion
{
    /// <summary>问题标识。</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>相关参数键。</summary>
    public string ParameterKey { get; set; } = string.Empty;

    /// <summary>问题文本。</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>问题类型。</summary>
    public CalculationParameterQuestionType QuestionType { get; set; }

    /// <summary>可选值。</summary>
    public List<string> Options { get; set; } =
        new List<string>();

    /// <summary>默认选项。</summary>
    public string DefaultOption { get; set; } = string.Empty;

    /// <summary>是否必须回答。</summary>
    public bool Required { get; set; }
}

/// <summary>参数解析诊断。</summary>
public sealed class CalculationParameterDiagnostic
{
    /// <summary>诊断代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>诊断级别。</summary>
    public CalculationDiagnosticSeverity Severity { get; set; }

    /// <summary>相关参数键。</summary>
    public string ParameterKey { get; set; } = string.Empty;

    /// <summary>诊断说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>相关原始文本。</summary>
    public string RawText { get; set; } = string.Empty;
}

/// <summary>参数解析和合并结果。</summary>
public sealed class CalculationParameterResolutionResult
{
    /// <summary>解析状态。</summary>
    public CalculationParameterResolutionStatus Status { get; set; } =
        CalculationParameterResolutionStatus.Resolved;

    /// <summary>应用参数后的计算方案。</summary>
    public CalculationSpec Spec { get; set; } =
        new CalculationSpec();

    /// <summary>最终规范参数。</summary>
    public List<ResolvedCalculationParameter> AppliedParameters
    {
        get;
        set;
    } = new List<ResolvedCalculationParameter>();

    /// <summary>需要用户回答的问题。</summary>
    public List<CalculationParameterQuestion> Questions { get; set; } =
        new List<CalculationParameterQuestion>();

    /// <summary>是否需要人工审批；首期始终为 false。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>解析诊断。</summary>
    public List<CalculationParameterDiagnostic> Diagnostics { get; set; } =
        new List<CalculationParameterDiagnostic>();
}
