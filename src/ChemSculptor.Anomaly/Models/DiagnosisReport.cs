namespace ChemSculptor.Anomaly.Models;

/// <summary>对一条异常的原因诊断。</summary>
public sealed class DiagnosisReport
{
    /// <summary>被诊断的异常代码。</summary>
    public string AnomalyCode { get; set; } = string.Empty;

    /// <summary>诊断摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>最可能的主要原因。</summary>
    public string PrimaryCause { get; set; } = string.Empty;

    /// <summary>诊断置信度，范围为 0 到 1。</summary>
    public double Confidence { get; set; }

    /// <summary>可能的次要原因或诱因。</summary>
    public List<string> ContributingFactors { get; set; } =
        new List<string>();

    /// <summary>诊断使用的证据。</summary>
    public List<AnomalyEvidence> Evidence { get; set; } =
        new List<AnomalyEvidence>();
}
