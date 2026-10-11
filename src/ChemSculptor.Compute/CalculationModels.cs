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

/// <summary>计算结果验证状态。</summary>
public enum CalculationValidationStatus
{
    /// <summary>尚未验证。</summary>
    NotEvaluated,

    /// <summary>验证通过。</summary>
    Passed,

    /// <summary>验证通过，但存在警告。</summary>
    PassedWithWarnings,

    /// <summary>验证失败。</summary>
    Failed
}

/// <summary>验证项目在整体判定中的要求等级。</summary>
public enum CalculationValidationRequirement
{
    /// <summary>必须通过；失败会阻止结果通过。</summary>
    Required,

    /// <summary>建议通过；失败会产生警告。</summary>
    Recommended,

    /// <summary>信息性检查；不直接改变通过状态。</summary>
    Informational
}

/// <summary>验证项目所属范围。</summary>
public enum CalculationValidationScope
{
    /// <summary>结果和作业的一般结构一致性。</summary>
    Structure,

    /// <summary>计算程序是否正常终结。</summary>
    ProgramOutput,

    /// <summary>能量、梯度或其他数值是否有效。</summary>
    Numerical,

    /// <summary>科学合理性判断。</summary>
    ScientificPlausibility,

    /// <summary>多个结果之间的一致性。</summary>
    CrossResultConsistency,

    /// <summary>其他范围。</summary>
    Other
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
    Unknown,

    /// <summary>SCF 没有收敛。</summary>
    ScfNotConverged
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

    /// <summary>电子态研究目标。</summary>
    public ElectronicStateObjective ElectronicStateObjective { get; set; } =
        ElectronicStateObjective.GroundState;

    /// <summary>指定自旋态目标的多重度；未指定时为空。</summary>
    public int? TargetMultiplicity { get; set; }

    /// <summary>指定激发态标签，例如 S1 或 T1；未指定时为空。</summary>
    public string TargetStateLabel { get; set; } = string.Empty;

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

    /// <summary>电子态研究目标；默认寻找基态。</summary>
    public ElectronicStateObjective ElectronicStateObjective { get; set; } =
        ElectronicStateObjective.GroundState;

    /// <summary>指定自旋态目标的多重度；未指定时为空。</summary>
    public int? TargetMultiplicity { get; set; }

    /// <summary>指定激发态标签，例如 S1 或 T1；未指定时为空。</summary>
    public string TargetStateLabel { get; set; } = string.Empty;

    /// <summary>本次计算使用的分子坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

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

    /// <summary>本次计算的科研目标。</summary>
    public string Goal { get; set; } = string.Empty;

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

    /// <summary>父作业标识；普通作业为空。</summary>
    public string ParentJobId { get; set; } = string.Empty;

    /// <summary>根工作流标识；普通作业为空。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>恢复尝试序号；普通作业为 0。</summary>
    public int AttemptNumber { get; set; }

    /// <summary>产生该派生作业的修正计划标识。</summary>
    public string CorrectionPlanId { get; set; } = string.Empty;

    /// <summary>使用的几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>本次计算的科研目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>计算方案。</summary>
    public CalculationSpec Spec { get; set; } = new CalculationSpec();

    /// <summary>作业状态。</summary>
    public CalculationJobState State { get; set; } = CalculationJobState.Created;

    /// <summary>工作区目录。</summary>
    public string WorkspaceDirectory { get; set; } = string.Empty;

    /// <summary>本次作业的运行目录。</summary>
    public string RunDirectory { get; set; } = string.Empty;

    /// <summary>input 目录中的原始输入文件路径。</summary>
    public string SourceInputFilePath { get; set; } = string.Empty;

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

    /// <summary>SCF 是否收敛；程序没有报告时为空。</summary>
    public bool? ScfConverged { get; set; }

    /// <summary>最终 SCF 迭代次数；程序没有报告时为空。</summary>
    public int? ScfIterations { get; set; }

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

    /// <summary>
    /// 本次计算形成的产物清单。
    /// 清单随 result.json 一起保存，供下载、备份和后续恢复使用。
    /// </summary>
    public List<CalculationArtifactDescriptor> Artifacts { get; set; } =
        new List<CalculationArtifactDescriptor>();
}

/// <summary>计算产物在后续处理中的用途。</summary>
public enum CalculationArtifactKind
{
    /// <summary>计算程序使用的输入文件。</summary>
    Input,

    /// <summary>计算程序产生的主要输出。</summary>
    PrimaryOutput,

    /// <summary>辅助输出或中间结果。</summary>
    SupportingOutput,

    /// <summary>可用于恢复或继续计算的状态文件。</summary>
    RestartState,

    /// <summary>其他文件。</summary>
    Other
}

/// <summary>
/// 计算产物发现上下文。
/// 具体程序适配器根据作业状态和已解析结果决定候选产物规则。
/// </summary>
public sealed class CalculationArtifactDiscoveryContext
{
    /// <summary>当前计算作业。</summary>
    public CalculationJob Job { get; set; } =
        new CalculationJob();

    /// <summary>已经解析出的通用计算结果。</summary>
    public CalculationResult Result { get; set; } =
        new CalculationResult();
}

/// <summary>
/// 适配器声明的产物匹配规则。
/// 通用层只认识这些规则和用途，不直接理解具体程序的扩展名。
/// </summary>
public sealed class CalculationArtifactPattern
{
    /// <summary>文件名或通配符模式。</summary>
    public string FilePattern { get; set; } = string.Empty;

    /// <summary>产物用途。</summary>
    public CalculationArtifactKind Kind { get; set; } = CalculationArtifactKind.Other;

    /// <summary>传输时使用的媒体类型。</summary>
    public string MediaType { get; set; } = "application/octet-stream";

    /// <summary>是否可以用于恢复或继续计算。</summary>
    public bool CanUseForRestart { get; set; }
}

/// <summary>
/// 一条已落盘的产物描述。
/// 使用相对路径，避免把服务器本机目录结构写入可迁移的结果文件。
/// </summary>
public sealed class CalculationArtifactDescriptor
{
    /// <summary>文件名称。</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>相对于计算运行目录的路径。</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>产物用途。</summary>
    public CalculationArtifactKind Kind { get; set; } = CalculationArtifactKind.Other;

    /// <summary>传输时使用的媒体类型。</summary>
    public string MediaType { get; set; } = "application/octet-stream";

    /// <summary>文件字节数。</summary>
    public long Length { get; set; }

    /// <summary>文件的 SHA-256 摘要。</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>是否可以用于恢复或继续计算。</summary>
    public bool CanUseForRestart { get; set; }
}

/// <summary>单点计算服务提交结果。</summary>
public sealed class SinglePointCalculationSubmissionResult
{
    /// <summary>是否成功提交。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>已创建的作业。</summary>
    public CalculationJob? Job { get; set; }

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
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

/// <summary>一条结构化验证检查。</summary>
public sealed class CalculationValidationCheck
{
    /// <summary>检查代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>检查说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>检查级别。</summary>
    public CalculationDiagnosticSeverity Severity { get; set; } =
        CalculationDiagnosticSeverity.Info;

    /// <summary>要求等级。</summary>
    public CalculationValidationRequirement Requirement { get; set; } =
        CalculationValidationRequirement.Required;

    /// <summary>验证范围。</summary>
    public CalculationValidationScope Scope { get; set; } =
        CalculationValidationScope.Other;

    /// <summary>期望值。</summary>
    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>实际值。</summary>
    public string ActualValue { get; set; } = string.Empty;

    /// <summary>检查说明或失败原因。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>参数校验报告。</summary>
public sealed class CalculationValidationReport
{
    /// <summary>是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>验证状态。</summary>
    public CalculationValidationStatus Status { get; set; } =
        CalculationValidationStatus.NotEvaluated;

    /// <summary>验证摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>验证器名称。</summary>
    public string ValidatorName { get; set; } = string.Empty;

    /// <summary>验证时间。</summary>
    public DateTimeOffset ValidatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>结构化检查列表。</summary>
    public List<CalculationValidationCheck> Checks { get; set; } =
        new List<CalculationValidationCheck>();

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
