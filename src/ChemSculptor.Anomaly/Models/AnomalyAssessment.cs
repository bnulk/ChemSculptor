namespace ChemSculptor.Anomaly.Models;

/// <summary>一次计算作业的异常评估结果。</summary>
public sealed class AnomalyAssessment
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>发现的所有异常。</summary>
    public List<AnomalyFinding> Findings { get; set; } =
        new List<AnomalyFinding>();

    /// <summary>评估时间。</summary>
    public DateTimeOffset AssessedAt { get; set; } = DateTimeOffset.UtcNow;
}
