namespace ChemSculptor.Anomaly.Models;

/// <summary>一条已经发现的异常。</summary>
public sealed class AnomalyFinding
{
    /// <summary>异常代码，例如 wavefunction-stability。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>面向人的短标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>异常分类。</summary>
    public AnomalyCategory Category { get; set; } = AnomalyCategory.Unknown;

    /// <summary>严重程度。</summary>
    public AnomalySeverity Severity { get; set; } = AnomalySeverity.Info;

    /// <summary>当前发现的置信度，范围为 0 到 1。</summary>
    public double Confidence { get; set; }

    /// <summary>是否阻止继续执行。</summary>
    public bool IsBlocking { get; set; }

    /// <summary>是否需要科学判断或人工审批。</summary>
    public bool RequiresScientificJudgment { get; set; }

    /// <summary>支持该发现的证据。</summary>
    public List<AnomalyEvidence> Evidence { get; set; } =
        new List<AnomalyEvidence>();

    /// <summary>补充信息。</summary>
    public Dictionary<string, string> Details { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
