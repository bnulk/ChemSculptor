using ChemSculptor.Compute;
using ChemSculptor.Conversation;

namespace ChemSculptor.Agent;

/// <summary>客户端发送给智能体的原始消息。</summary>
public sealed class AgentRequest
{
    /// <summary>会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>用户原始文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>当前坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;
}

/// <summary>直接执行单点计算的请求。</summary>
public sealed class AgentSinglePointRequest
{
    /// <summary>分子坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;
}

/// <summary>智能体处理结果。</summary>
public sealed class AgentResult
{
    /// <summary>是否成功处理并支持该请求。</summary>
    public bool IsSupported { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>解释出的任务类型。</summary>
    public CalculationTaskType? TaskType { get; set; }

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

    /// <summary>需要用户回答的问题。</summary>
    public List<ConversationQuestion> Questions { get; set; } =
        new List<ConversationQuestion>();

    /// <summary>本次解析应用的计算参数。</summary>
    public List<ResolvedCalculationParameter> AppliedParameters
    {
        get;
        set;
    } = new List<ResolvedCalculationParameter>();

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

    /// <summary>波函数稳定性检查状态。</summary>
    public string WavefunctionStabilityStatus { get; set; } =
        string.Empty;

    /// <summary>波函数稳定性检查摘要。</summary>
    public string WavefunctionStabilitySummary { get; set; } =
        string.Empty;
}
