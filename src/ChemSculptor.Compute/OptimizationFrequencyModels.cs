namespace ChemSculptor.Compute;

/// <summary>几何优化和振动分析联合计算请求。</summary>
public sealed class OptimizationFrequencyRequest
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>用户自然语言目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>输入坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>已经解析完成的联合计算方案。</summary>
    public CalculationSpec? ResolvedSpec { get; set; }

    /// <summary>已经解析完成的规范参数。</summary>
    public List<ResolvedCalculationParameter> ResolvedParameters
    {
        get;
        set;
    } = new List<ResolvedCalculationParameter>();
}

/// <summary>联合优化频率计算提交结果。</summary>
public sealed class OptimizationFrequencySubmissionResult
{
    /// <summary>是否成功完成。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>已创建的作业。</summary>
    public CalculationJob? Job { get; set; }

    /// <summary>联合计算结果。</summary>
    public OptimizationFrequencyCalculationSkillResult? Result
    {
        get;
        set;
    }
}
