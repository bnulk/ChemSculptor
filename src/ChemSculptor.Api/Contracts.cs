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

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; } = 1;
}

/// <summary>单点计算触发结果。</summary>
public sealed class SinglePointCalculationResponse
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

/// <summary>计算触发失败时的响应。</summary>
public sealed class CalculationErrorResponse
{
    /// <summary>错误说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}
