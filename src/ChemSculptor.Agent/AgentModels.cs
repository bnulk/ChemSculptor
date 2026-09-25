using ChemSculptor.Compute;

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

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; } = 1;
}

/// <summary>直接执行单点计算的请求。</summary>
public sealed class AgentSinglePointRequest
{
    /// <summary>分子坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; } = 1;
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

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}
