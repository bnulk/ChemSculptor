namespace ChemSculptor.Anomaly.Models;

/// <summary>通用波函数稳定性检查结果。</summary>
public sealed class WavefunctionStabilityResult
{
    /// <summary>稳定性状态。</summary>
    public WavefunctionStabilityStatus Status { get; set; } =
        WavefunctionStabilityStatus.NotPerformed;

    /// <summary>不稳定类型，例如 internal 或 external。</summary>
    public string InstabilityKind { get; set; } = string.Empty;

    /// <summary>结果摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>稳定性检查输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>稳定性检查证据。</summary>
    public List<AnomalyEvidence> Evidence { get; set; } =
        new List<AnomalyEvidence>();

    /// <summary>执行稳定性检查时使用的自旋多重度。</summary>
    public int CurrentMultiplicity { get; set; }

    /// <summary>稳定性矩阵的全部本征向量。</summary>
    public List<WavefunctionStabilityEigenvector> Eigenvectors { get; set; } =
        new List<WavefunctionStabilityEigenvector>();
}
