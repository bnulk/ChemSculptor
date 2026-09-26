namespace ChemSculptor.Compute.Gaussian;

/// <summary>Gaussian 专用处理动作类型。</summary>
public enum GaussianProcessingActionKind
{
    /// <summary>无需动作。</summary>
    None,

    /// <summary>检查 Gaussian 输出和错误信息。</summary>
    InspectOutput,

    /// <summary>使用相同 Gaussian 输入重新运行。</summary>
    RerunSameInput,

    /// <summary>修改 Gaussian 输入后重新运行。</summary>
    ModifyInput,

    /// <summary>请求用户作出决定。</summary>
    RequestUserDecision,

    /// <summary>停止当前 Gaussian 作业。</summary>
    Abort
}

/// <summary>Gaussian 专用处理动作。</summary>
public sealed class GaussianProcessingAction
{
    /// <summary>动作代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gaussian 专用动作类型。</summary>
    public GaussianProcessingActionKind Kind { get; set; } =
        GaussianProcessingActionKind.None;

    /// <summary>动作说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否必须经过人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>动作参数。</summary>
    public Dictionary<string, string> Parameters { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Gaussian 专用处理方案。</summary>
public sealed class GaussianProcessingPlan
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>方案摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Gaussian 专用处理动作。</summary>
    public List<GaussianProcessingAction> Actions { get; set; } =
        new List<GaussianProcessingAction>();

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
