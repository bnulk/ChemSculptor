namespace ChemSculptor.ScientificData.Models;

/// <summary>计算点的来源和接受过程。</summary>
public sealed class PointProvenance
{
    /// <summary>第一次计算作业标识。</summary>
    public string InitialJobId { get; set; } = string.Empty;

    /// <summary>最终被接受的作业标识。</summary>
    public string AcceptedJobId { get; set; } = string.Empty;

    /// <summary>该点所属的根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>来源点标识；没有来源点时为空。</summary>
    public string ParentPointId { get; set; } = string.Empty;

    /// <summary>产生当前接受点所使用的修正次数。</summary>
    public int CorrectionCount { get; set; }

    /// <summary>接受该点的规则、角色或审查者。</summary>
    public string AcceptedBy { get; set; } = string.Empty;

    /// <summary>接受该点的简明说明。</summary>
    public string AcceptanceSummary { get; set; } = string.Empty;
}

/// <summary>计算点上执行过的一项验证。</summary>
public sealed class PointValidationRecord
{
    /// <summary>验证标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>验证代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>验证状态。</summary>
    public PointValidationStatus Status { get; set; } =
        PointValidationStatus.NotRun;

    /// <summary>验证说明。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>该项验证是否属于必要条件。</summary>
    public bool IsRequired { get; set; }

    /// <summary>执行验证的作业标识；没有作业时为空。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>验证时间。</summary>
    public DateTimeOffset ValidatedAt { get; set; } =
        DateTimeOffset.UtcNow;
}
