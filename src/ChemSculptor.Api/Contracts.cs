namespace ChemSculptor.Api;

/// <summary>人工干预请求。</summary>
public sealed class InterveneRequest
{
    public string Operation { get; set; } = string.Empty;

    public string? NodeId { get; set; }

    public string? Parameter { get; set; }

    public string? Value { get; set; }
}

/// <summary>人工审批请求。</summary>
public sealed class ApprovalRequest
{
    public bool Approved { get; set; }

    public string? Note { get; set; }
}

/// <summary>注册技能的请求。</summary>
public sealed class RegisterSkillRequest
{
    public string Id { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0.0";

    public List<string> Capabilities { get; set; } = new List<string>();
}

/// <summary>通用错误响应。</summary>
public sealed class ApiError
{
    public string Error { get; set; } = string.Empty;
}

/// <summary>根端点返回的服务说明。</summary>
public sealed class ServiceInfoResponse
{
    public string Service { get; set; } = string.Empty;

    public List<string> Endpoints { get; set; } = new List<string>();
}

/// <summary>人工干预端点的响应。</summary>
public sealed class InterventionResponse
{
    public string Id { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string? NodeId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

/// <summary>审批端点的响应。</summary>
public sealed class ApprovalResponse
{
    public string Id { get; set; } = string.Empty;

    public bool Approved { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}

/// <summary>客户端任务创建成功的响应。</summary>
public sealed class ClientJobAcceptedResponse
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }
}

/// <summary>客户端任务状态响应。</summary>
public sealed class ClientJobStatusResponse
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool HasResult { get; set; }
}

/// <summary>坐标响应中的单个原子。</summary>
public sealed class GeometryAtomResponse
{
    public string Element { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }
}

/// <summary>坐标接收成功后的响应。</summary>
public sealed class GeometrySubmitResponse
{
    public string SourceName { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public int AtomCount { get; set; }

    public List<GeometryAtomResponse> Atoms { get; set; } = new List<GeometryAtomResponse>();

    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>坐标解析失败时的响应。</summary>
public sealed class GeometryErrorResponse
{
    public string Error { get; set; } = string.Empty;

    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>触发单点计算的请求。</summary>
public sealed class SinglePointCalculationRequest
{
    /// <summary>分子坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>客户提交的原始自然语言文本。</summary>
    public string Text { get; set; } = string.Empty;
}

/// <summary>单点计算触发结果。</summary>
public sealed class SinglePointCalculationResponse
{
    /// <summary>是否成功提交。</summary>
    public bool Succeeded { get; set; }

    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>当前状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>生成的输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>计算输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>取消计算作业响应。</summary>
public sealed class CalculationCancelResponse
{
    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>是否接受取消。</summary>
    public bool Canceled { get; set; }

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>科学点文件成果包清单响应。</summary>
public sealed class ScientificArtifactManifestResponse
{
    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>根计算作业标识。</summary>
    public string RootJobId { get; set; } = string.Empty;

    /// <summary>科学点清单。</summary>
    public List<ScientificArtifactPointResponse> Points { get; set; } =
        new List<ScientificArtifactPointResponse>();
}

/// <summary>成果包中的单个科学点。</summary>
public sealed class ScientificArtifactPointResponse
{
    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>科学点在成果中的顺序。</summary>
    public int Sequence { get; set; }

    /// <summary>成果包中使用的科学点目录名。</summary>
    public string DirectoryName { get; set; } = string.Empty;

    /// <summary>科学点接受状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>该科学点的文件清单。</summary>
    public List<ScientificArtifactFileResponse> Files { get; set; } =
        new List<ScientificArtifactFileResponse>();
}

/// <summary>成果包中的单个文件。</summary>
public sealed class ScientificArtifactFileResponse
{
    /// <summary>文件引用标识。</summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>文件类别。</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>客户端保存时使用的文件名。</summary>
    public string DownloadFileName { get; set; } = string.Empty;

    /// <summary>文件字节数。</summary>
    public long Length { get; set; }

    /// <summary>文件引用中记录的 SHA-256 摘要。</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>文件当前是否可下载。</summary>
    public bool IsAvailable { get; set; }

    /// <summary>文件不可用时的说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>下载该文件的 API 路径。</summary>
    public string DownloadPath { get; set; } = string.Empty;
}

/// <summary>计算作业状态响应。</summary>
public sealed class CalculationStatusResponse
{
    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>当前作业状态。</summary>
    public string State { get; set; } = string.Empty;

    /// <summary>开始时间。</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>完成时间。</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>原始输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>程序输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnosticResponse> Diagnostics { get; set; } =
        new List<CalculationDiagnosticResponse>();
}

/// <summary>规范化计算结果响应。</summary>
public sealed class CalculationResultResponse
{
    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>最终能量。</summary>
    public double? Energy { get; set; }

    /// <summary>能量单位。</summary>
    public string EnergyUnit { get; set; } = string.Empty;

    /// <summary>程序是否正常结束。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>通用失败类别。</summary>
    public string FailureKind { get; set; } = string.Empty;

    /// <summary>计算程序。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>方法或泛函。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>基组。</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>程序输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnosticResponse> Diagnostics { get; set; } =
        new List<CalculationDiagnosticResponse>();

}

/// <summary>计算结果验证响应。</summary>
public sealed class CalculationValidationResponse
{
    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>是否通过验证。</summary>
    public bool Passed { get; set; }

    /// <summary>验证状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>验证摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>验证器名称。</summary>
    public string ValidatorName { get; set; } = string.Empty;

    /// <summary>验证时间。</summary>
    public DateTimeOffset ValidatedAt { get; set; }

    /// <summary>结构化检查结果。</summary>
    public List<CalculationValidationCheckResponse> Checks { get; set; } =
        new List<CalculationValidationCheckResponse>();

    /// <summary>验证问题。</summary>
    public List<CalculationDiagnosticResponse> Issues { get; set; } =
        new List<CalculationDiagnosticResponse>();
}

/// <summary>单条计算结果验证检查响应。</summary>
public sealed class CalculationValidationCheckResponse
{
    /// <summary>检查代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>检查说明。</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>检查级别。</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>要求等级。</summary>
    public string Requirement { get; set; } = string.Empty;

    /// <summary>验证范围。</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>期望值。</summary>
    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>实际值。</summary>
    public string ActualValue { get; set; } = string.Empty;

    /// <summary>检查说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>计算诊断信息响应。</summary>
public sealed class CalculationDiagnosticResponse
{
    /// <summary>严重程度。</summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>诊断代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>面向人的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>计算触发失败时的响应。</summary>
public sealed class CalculationErrorResponse
{
    /// <summary>错误说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>客户端发送给智能体的原始消息。</summary>
public sealed class AgentMessageRequest
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>用户原始自然语言文本，不做客户端解释。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>当前选择的坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;
}

/// <summary>智能体消息处理结果。</summary>
public sealed class AgentMessageResponse
{
    /// <summary>服务器解释出的任务类型。</summary>
    public string TaskType { get; set; } = string.Empty;

    /// <summary>服务器选择的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>吸收光谱任务的前置处理模式。</summary>
    public string AbsorptionSpectrumMode { get; set; } =
        string.Empty;

    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>当前状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>生成的输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>计算输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();

    /// <summary>当前是否需要用户继续操作。</summary>
    public bool RequiresUserAction { get; set; }

    /// <summary>需要用户回答的参数问题。</summary>
    public List<AgentQuestionResponse> Questions { get; set; } =
        new List<AgentQuestionResponse>();

    /// <summary>本次解析应用的参数。</summary>
    public List<AgentAppliedParameterResponse> AppliedParameters
    {
        get;
        set;
    } = new List<AgentAppliedParameterResponse>();

    /// <summary>优化是否收敛。</summary>
    public bool? OptimizationConverged { get; set; }

    /// <summary>优化步骤数量。</summary>
    public int OptimizationStepCount { get; set; }

    /// <summary>优化最终能量。</summary>
    public double? FinalEnergy { get; set; }

    /// <summary>最终结构摘要。</summary>
    public string FinalStructure { get; set; } = string.Empty;

    /// <summary>最大力。</summary>
    public double? MaximumForce { get; set; }

    /// <summary>RMS 力。</summary>
    public double? RmsForce { get; set; }

    /// <summary>最大位移。</summary>
    public double? MaximumDisplacement { get; set; }

    /// <summary>RMS 位移。</summary>
    public double? RmsDisplacement { get; set; }

    /// <summary>优化恢复修正次数。</summary>
    public int CorrectionCount { get; set; }

    /// <summary>最终采用的作业标识。</summary>
    public string FinalJobId { get; set; } = string.Empty;

    /// <summary>优化阶段最终采用的作业标识。</summary>
    public string OptimizationJobId { get; set; } = string.Empty;

    /// <summary>TDDFT 阶段作业标识。</summary>
    public string TddftJobId { get; set; } = string.Empty;

    /// <summary>吸收光谱科学成果标识。</summary>
    public string ScientificResultId { get; set; } = string.Empty;

    /// <summary>NTO 选择模式。</summary>
    public string NtoSelectionMode { get; set; } = string.Empty;

    /// <summary>NTO 选中的激发态数量。</summary>
    public int NtoSelectedStateCount { get; set; }

    /// <summary>NTO 派生作业标识列表。</summary>
    public List<string> NtoJobIds { get; set; } =
        new List<string>();

    /// <summary>解析出的激发态数量。</summary>
    public int ExcitedStateCount { get; set; }

    /// <summary>最强吸收对应激发态序号。</summary>
    public int StrongestExcitedStateIndex { get; set; }

    /// <summary>最强吸收对应波长，单位 nm。</summary>
    public double? MaximumWavelengthNm { get; set; }

    /// <summary>最强吸收对应振子强度。</summary>
    public double? MaximumOscillatorStrength { get; set; }

    /// <summary>波函数稳定性检查状态。</summary>
    public string WavefunctionStabilityStatus { get; set; } =
        string.Empty;

    /// <summary>波函数稳定性检查摘要。</summary>
    public string WavefunctionStabilitySummary { get; set; } =
        string.Empty;

    /// <summary>振动分析状态。</summary>
    public string FrequencyAnalysisStatus { get; set; } =
        string.Empty;

    /// <summary>频率数量。</summary>
    public int FrequencyCount { get; set; }

    /// <summary>虚频数量。</summary>
    public int ImaginaryFrequencyCount { get; set; }

    /// <summary>最低频率。</summary>
    public double? MinimumFrequency { get; set; }

    /// <summary>是否为稳定最小点。</summary>
    public bool? IsMinimum { get; set; }
}

/// <summary>智能体返回的参数问题。</summary>
public sealed class AgentQuestionResponse
{
    public string QuestionId { get; set; } = string.Empty;

    public string QuestionType { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public List<string> Options { get; set; } = new List<string>();

    public string RiskLevel { get; set; } = string.Empty;
}

/// <summary>智能体返回的已应用参数。</summary>
public sealed class AgentAppliedParameterResponse
{
    public string Key { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public double Confidence { get; set; }

    public string RiskLevel { get; set; } = string.Empty;

    public bool RequiresApproval { get; set; }

    public string TranslatedKeyword { get; set; } = string.Empty;
}
