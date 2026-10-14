namespace ChemSculptor.Compute;

/// <summary>吸收光谱任务的前置处理模式。</summary>
public enum AbsorptionSpectrumMode
{
    /// <summary>先做完整几何优化，再做 TDDFT 单点计算。</summary>
    OptimizeThenTddft,

    /// <summary>跳过几何优化，直接在输入结构上做 TDDFT 单点计算。</summary>
    TddftOnly,

    /// <summary>冻结非氢原子后做几何优化，再做 TDDFT 单点计算。</summary>
    ConstrainedOptimizationThenTddft
}

/// <summary>吸收光谱工作流失败阶段。</summary>
public enum AbsorptionSpectrumFailureStage
{
    /// <summary>优化阶段。</summary>
    Optimization,

    /// <summary>TDDFT 阶段。</summary>
    Tddft,

    /// <summary>科学数据归档阶段。</summary>
    Archive
}

/// <summary>吸收光谱异常错误码。</summary>
public static class AbsorptionSpectrumAnomalyCodes
{
    /// <summary>优化阶段失败。</summary>
    public const string OptimizationFailed =
        "absorption.optimization.failed";

    /// <summary>TDDFT 阶段失败。</summary>
    public const string TddftFailed =
        "absorption.tddft.failed";

    /// <summary>归档阶段失败。</summary>
    public const string ArchiveFailed =
        "absorption.archive.failed";

    /// <summary>尚未实现自动恢复策略。</summary>
    public const string RecoveryNotImplemented =
        "absorption.recovery.not_implemented";
}

/// <summary>吸收光谱恢复规划上下文。</summary>
public sealed class AbsorptionSpectrumRecoveryContext
{
    /// <summary>原始请求。</summary>
    public AbsorptionSpectrumRequest Request { get; set; } =
        new AbsorptionSpectrumRequest();

    /// <summary>失败阶段。</summary>
    public AbsorptionSpectrumFailureStage Stage { get; set; }

    /// <summary>稳定错误码。</summary>
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>已经执行的优化结果。</summary>
    public OptimizationSubmissionResult? Optimization { get; set; }

    /// <summary>已经执行的 TDDFT 结果。</summary>
    public TddftSubmissionResult? Tddft { get; set; }
}

/// <summary>吸收光谱恢复计划。</summary>
public sealed class AbsorptionSpectrumRecoveryPlan
{
    /// <summary>是否已生成可执行计划。</summary>
    public bool PlanCreated { get; set; }

    /// <summary>恢复策略标识。</summary>
    public string StrategyId { get; set; } = string.Empty;

    /// <summary>恢复计划说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否允许直接重试。</summary>
    public bool CanRetry { get; set; }

    /// <summary>恢复参数。</summary>
    public Dictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}

/// <summary>吸收光谱工作流请求。</summary>
public sealed class AbsorptionSpectrumRequest
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>用户自然语言目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>输入坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>吸收光谱前置处理模式。</summary>
    public AbsorptionSpectrumMode Mode { get; set; } =
        AbsorptionSpectrumMode.OptimizeThenTddft;

    /// <summary>已经解析完成的 TDDFT 计算方案。</summary>
    public CalculationSpec? TddftSpec { get; set; }

    /// <summary>已经解析完成的 TDDFT 规范参数。</summary>
    public List<ResolvedCalculationParameter> ResolvedParameters
    {
        get;
        set;
    } = new List<ResolvedCalculationParameter>();
}

/// <summary>吸收光谱工作流提交结果。</summary>
public sealed class AbsorptionSpectrumSubmissionResult
{
    /// <summary>工作流是否成功完成。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>实际执行的吸收光谱模式。</summary>
    public AbsorptionSpectrumMode Mode { get; set; }

    /// <summary>优化阶段结果；直接 TDDFT 模式时为空。</summary>
    public OptimizationSubmissionResult? Optimization { get; set; }

    /// <summary>TDDFT 单点阶段结果。</summary>
    public TddftSubmissionResult? Tddft { get; set; }

    /// <summary>失败阶段；成功时为空。</summary>
    public AbsorptionSpectrumFailureStage? FailureStage
    {
        get;
        set;
    }

    /// <summary>稳定失败错误码。</summary>
    public string FailureCode { get; set; } = string.Empty;

    /// <summary>吸收光谱恢复计划。</summary>
    public AbsorptionSpectrumRecoveryPlan? RecoveryPlan
    {
        get;
        set;
    }

    /// <summary>吸收光谱科学成果标识。</summary>
    public string ScientificResultId { get; set; } = string.Empty;
}

/// <summary>吸收光谱科学数据归档请求。</summary>
public sealed class AbsorptionSpectrumArchiveRequest
{
    /// <summary>吸收光谱工作流请求。</summary>
    public AbsorptionSpectrumRequest Request { get; set; } =
        new AbsorptionSpectrumRequest();

    /// <summary>吸收光谱工作流结果。</summary>
    public AbsorptionSpectrumSubmissionResult Result { get; set; } =
        new AbsorptionSpectrumSubmissionResult();
}

/// <summary>吸收光谱科学数据归档结果。</summary>
public sealed class AbsorptionSpectrumArchiveResult
{
    /// <summary>是否成功归档。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>生成的科学成果标识。</summary>
    public string ScientificResultId { get; set; } = string.Empty;
}
