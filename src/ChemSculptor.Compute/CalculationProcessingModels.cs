namespace ChemSculptor.Compute;

/// <summary>通用计算处理结果。</summary>
public enum CalculationProcessingOutcome
{
    /// <summary>计算和处理均已完成。</summary>
    Completed,

    /// <summary>计算完成，但存在需要注意的信息。</summary>
    CompletedWithWarnings,

    /// <summary>需要修改输入或设置后重试。</summary>
    NeedsRetry,

    /// <summary>需要用户补充信息或作出决定。</summary>
    NeedsUserInput,

    /// <summary>当前无法继续处理。</summary>
    Failed
}

/// <summary>通用处理动作类型。</summary>
public enum CalculationProcessingActionType
{
    /// <summary>无需动作。</summary>
    None,

    /// <summary>检查输出和诊断。</summary>
    ReviewOutput,

    /// <summary>使用原输入重新计算。</summary>
    RetryAsIs,

    /// <summary>修改输入或设置后重试。</summary>
    RetryWithChanges,

    /// <summary>请求用户补充信息或审批。</summary>
    RequestUserInput,

    /// <summary>停止处理。</summary>
    Abort
}

/// <summary>通用处理动作。</summary>
public sealed class CalculationProcessingAction
{
    /// <summary>动作代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>动作类型。</summary>
    public CalculationProcessingActionType ActionType { get; set; } =
        CalculationProcessingActionType.None;

    /// <summary>面向人的标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>动作说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否必须经过人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>动作参数。</summary>
    public Dictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>通用计算处理方案。</summary>
public sealed class CalculationProcessingPlan
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>总体处理结果。</summary>
    public CalculationProcessingOutcome Outcome { get; set; } =
        CalculationProcessingOutcome.Failed;

    /// <summary>方案摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>处理动作列表。</summary>
    public List<CalculationProcessingAction> Actions { get; set; } =
        new List<CalculationProcessingAction>();

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>计算程序专用处理动作。</summary>
public sealed class ProgramProcessingAction
{
    /// <summary>动作代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>程序专用动作类型。</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>动作说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否必须经过人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>动作参数。</summary>
    public Dictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>计算程序专用处理方案。</summary>
public sealed class ProgramProcessingPlan
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>程序名称。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>方案摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>程序专用处理动作。</summary>
    public List<ProgramProcessingAction> Actions { get; set; } =
        new List<ProgramProcessingAction>();

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
