namespace ChemSculptor.WinForms;

/// <summary>
/// 服务端返回的客户端任务摘要。
/// </summary>
public sealed class ClientJobSummary
{
    public string Id { get; set; } = string.Empty;

    public string JobId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool HasResult { get; set; }
}

/// <summary>客户端任务列表中的一项。</summary>
public sealed class ClientJobItem
{
    public string Id { get; set; } = string.Empty;

    public string Status { get; set; } = "Queued";

    public string? ResultText { get; set; }

    public override string ToString()
    {
        return Id + "    [" + Status + "]";
    }
}

/// <summary>坐标响应中的单个原子。</summary>
public sealed class GeometryAtomDto
{
    public string Element { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public double Z { get; set; }
}

/// <summary>坐标接收响应。</summary>
public sealed class GeometrySubmitResult
{
    public string SourceName { get; set; } = string.Empty;

    public string Formula { get; set; } = string.Empty;

    public int AtomCount { get; set; }

    public List<GeometryAtomDto> Atoms { get; set; } = new List<GeometryAtomDto>();

    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>会话中的一条消息。</summary>
public sealed class ChatMessage
{
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.Now;
}

/// <summary>一个本地会话。</summary>
public sealed class ChatSession
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();

    public override string ToString()
    {
        return Title;
    }
}

/// <summary>触发单点计算的客户端请求。</summary>
public sealed class SinglePointCalculationRequestDto
{
    /// <summary>分子坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; } = 1;
}

/// <summary>触发单点计算后的客户端响应。</summary>
public sealed class SinglePointCalculationResultDto
{
    /// <summary>计算作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>当前状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>生成的输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>发送给服务器的智能体原始消息。</summary>
public sealed class AgentMessageRequestDto
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>用户原始自然语言文本。</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>当前选择的坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; } = 1;
}

/// <summary>服务器处理智能体消息后的响应。</summary>
public sealed class AgentMessageResultDto
{
    /// <summary>服务器解释出的任务类型。</summary>
    public string TaskType { get; set; } = string.Empty;

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
