namespace ChemSculptor.Compute;

/// <summary>计算任务类型。</summary>
public enum CalculationTaskType
{
    /// <summary>单点能计算。</summary>
    SinglePoint,

    /// <summary>几何优化。</summary>
    Optimization,

    /// <summary>频率计算。</summary>
    Frequency,

    /// <summary>激发态计算。</summary>
    TdDft,

    /// <summary>自旋轨道耦合计算。</summary>
    Soc,

    /// <summary>MECP 搜索。</summary>
    Mecp
}

/// <summary>参数来源。</summary>
public enum ParameterSource
{
    /// <summary>系统默认值。</summary>
    Default,

    /// <summary>用户明确指定。</summary>
    User,

    /// <summary>系统根据规则补充。</summary>
    System,

    /// <summary>智能体建议。</summary>
    Agent
}

/// <summary>参数风险等级。</summary>
public enum CalculationRiskLevel
{
    /// <summary>普通信息。</summary>
    Info,

    /// <summary>警告，需要提示。</summary>
    Warning,

    /// <summary>阻塞，必须人工审批。</summary>
    Blocking,

    /// <summary>禁止执行。</summary>
    Forbidden
}

/// <summary>计算作业状态。</summary>
public enum CalculationJobState
{
    /// <summary>已创建。</summary>
    Created,

    /// <summary>输入文件已生成。</summary>
    InputGenerated,

    /// <summary>已进入队列。</summary>
    Queued,

    /// <summary>正在运行。</summary>
    Running,

    /// <summary>程序已结束。</summary>
    Completed,

    /// <summary>输出已解析。</summary>
    Parsed,

    /// <summary>结果已验证。</summary>
    Validated,

    /// <summary>失败。</summary>
    Failed,

    /// <summary>已取消。</summary>
    Canceled
}

/// <summary>诊断级别。</summary>
public enum CalculationDiagnosticSeverity
{
    /// <summary>普通信息。</summary>
    Info,

    /// <summary>警告。</summary>
    Warning,

    /// <summary>错误。</summary>
    Error
}

/// <summary>通用计算失败类别。</summary>
public enum CalculationFailureKind
{
    /// <summary>没有失败。</summary>
    None,

    /// <summary>计算进程失败。</summary>
    ProcessFailed,

    /// <summary>输出文件不存在。</summary>
    OutputMissing,

    /// <summary>缺少正常结束标志。</summary>
    NormalTerminationMissing,

    /// <summary>计算程序报告错误结束。</summary>
    ProgramError,

    /// <summary>缺少最终能量。</summary>
    EnergyMissing,

    /// <summary>计算已取消。</summary>
    Canceled,

    /// <summary>无法分类的失败。</summary>
    Unknown
}

/// <summary>
/// 单个计算参数及其元信息。
/// 用于记录默认值、当前值、来源、风险等级和是否需要审批。
/// </summary>
public sealed class CalculationParameter
{
    /// <summary>参数内部名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>参数显示名称。</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>当前值。</summary>
    public string CurrentValue { get; set; } = string.Empty;

    /// <summary>默认值。</summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>当前值来源。</summary>
    public ParameterSource Source { get; set; } = ParameterSource.Default;

    /// <summary>风险等级。</summary>
    public CalculationRiskLevel RiskLevel { get; set; } = CalculationRiskLevel.Info;

    /// <summary>是否需要人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>参数说明。</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 一次计算方案。
/// 描述“怎么算”，不包含几何坐标和运行状态。
/// </summary>
public sealed class CalculationSpec
{
    /// <summary>任务类型。</summary>
    public CalculationTaskType TaskType { get; set; } = CalculationTaskType.SinglePoint;

    /// <summary>计算程序名称。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>方法或泛函。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>基组。</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>溶剂模型；为空表示气相。</summary>
    public string Solvent { get; set; } = string.Empty;

    /// <summary>额外程序参数。</summary>
    public Dictionary<string, string> ExtraOptions { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>参数列表及其来源和风险信息。</summary>
    public List<CalculationParameter> Parameters { get; set; } = new List<CalculationParameter>();
}

/// <summary>
/// 客户端提交的计算请求。
/// </summary>
public sealed class CalculationRequest
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>用户自然语言目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>用户覆盖的参数。</summary>
    public List<CalculationParameter> Overrides { get; set; } = new List<CalculationParameter>();
}

/// <summary>
/// 会话中的一个计算任务。
/// 一个任务对应一个计算作业。
/// </summary>
public sealed class CalculationTask
{
    /// <summary>任务标识。</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>所属会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>所属工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>使用的几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>计算方案。</summary>
    public CalculationSpec Spec { get; set; } = new CalculationSpec();

    /// <summary>任务状态。</summary>
    public CalculationJobState State { get; set; } = CalculationJobState.Created;

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 一次具体计算作业的运行记录。
/// </summary>
public sealed class CalculationJob
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>所属任务标识。</summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>所属会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>所属工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>使用的几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>计算方案。</summary>
    public CalculationSpec Spec { get; set; } = new CalculationSpec();

    /// <summary>作业状态。</summary>
    public CalculationJobState State { get; set; } = CalculationJobState.Created;

    /// <summary>工作区目录。</summary>
    public string WorkspaceDirectory { get; set; } = string.Empty;

    /// <summary>本次作业的运行目录。</summary>
    public string RunDirectory { get; set; } = string.Empty;

    /// <summary>输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>开始时间。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>结束时间。</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new List<CalculationDiagnostic>();
}

/// <summary>
/// 计算结果的规范化表示。
/// </summary>
public sealed class CalculationResult
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>最终能量；解析失败时为空。</summary>
    public double? Energy { get; set; }

    /// <summary>能量单位。</summary>
    public string EnergyUnit { get; set; } = "Hartree";

    /// <summary>程序是否正常结束。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>通用失败类别。</summary>
    public CalculationFailureKind FailureKind { get; set; } =
        CalculationFailureKind.None;

    /// <summary>计算程序名称。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>方法或泛函。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>基组。</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } = new List<CalculationDiagnostic>();
}

/// <summary>
/// 一条计算诊断信息。
/// </summary>
public sealed class CalculationDiagnostic
{
    /// <summary>严重程度。</summary>
    public CalculationDiagnosticSeverity Severity { get; set; } = CalculationDiagnosticSeverity.Info;

    /// <summary>诊断代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>面向人的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 执行上下文。
/// 描述程序如何运行，但不包含具体执行逻辑。
/// </summary>
public sealed class CalculationExecutionContext
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>工作区目录。</summary>
    public string WorkspaceDirectory { get; set; } = string.Empty;

    /// <summary>运行目录。</summary>
    public string RunDirectory { get; set; } = string.Empty;

    /// <summary>输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>程序启动参数。</summary>
    public List<string> Arguments { get; set; } = new List<string>();

    /// <summary>启动子进程时追加或覆盖的环境变量。</summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>可执行文件路径。</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>并行核数。</summary>
    public int ProcessorCount { get; set; }

    /// <summary>内存设置。</summary>
    public string Memory { get; set; } = string.Empty;

    /// <summary>检查点文件路径。</summary>
    public string CheckpointFilePath { get; set; } = string.Empty;
}

/// <summary>一条参数校验问题。</summary>
public sealed class CalculationValidationIssue
{
    /// <summary>严重程度。</summary>
    public CalculationDiagnosticSeverity Severity { get; set; } = CalculationDiagnosticSeverity.Info;

    /// <summary>问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>问题说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>参数校验报告。</summary>
public sealed class CalculationValidationReport
{
    /// <summary>是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>问题列表。</summary>
    public List<CalculationValidationIssue> Issues { get; set; } = new List<CalculationValidationIssue>();
}

/// <summary>科学风险评估结果。</summary>
public sealed class RiskAssessment
{
    /// <summary>风险等级。</summary>
    public CalculationRiskLevel Level { get; set; } = CalculationRiskLevel.Info;

    /// <summary>是否需要人工审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>风险摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>风险详情。</summary>
    public List<string> Details { get; set; } = new List<string>();

    /// <summary>建议操作。</summary>
    public List<string> SuggestedActions { get; set; } = new List<string>();
}

/// <summary>向客户端提出的问题。</summary>
public sealed class CalculationQuestion
{
    /// <summary>问题标识。</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>问题类型。</summary>
    public string QuestionType { get; set; } = string.Empty;

    /// <summary>问题标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>问题说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>风险等级。</summary>
    public CalculationRiskLevel RiskLevel { get; set; } = CalculationRiskLevel.Info;

    /// <summary>可选项。</summary>
    public List<string> Options { get; set; } = new List<string>();
}

/// <summary>计算计划。</summary>
public sealed class CalculationPlan
{
    /// <summary>会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>计算方案。</summary>
    public CalculationSpec Spec { get; set; } = new CalculationSpec();

    /// <summary>是否需要审批。</summary>
    public bool RequiresApproval { get; set; }

    /// <summary>需要用户回答的问题。</summary>
    public List<CalculationQuestion> Questions { get; set; } = new List<CalculationQuestion>();
}

/// <summary>人工审批决定。</summary>
public sealed class ApprovalDecision
{
    /// <summary>问题标识。</summary>
    public string QuestionId { get; set; } = string.Empty;

    /// <summary>是否批准。</summary>
    public bool Approved { get; set; }

    /// <summary>用户修改内容。</summary>
    public List<CalculationParameter> Changes { get; set; } = new List<CalculationParameter>();

    /// <summary>用户说明。</summary>
    public string Reason { get; set; } = string.Empty;
}
