using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>异常检测器。</summary>
public interface IAnomalyDetector
{
    /// <summary>异常代码。</summary>
    string Code { get; }

    /// <summary>检测器版本。</summary>
    string Version { get; }

    /// <summary>判断当前上下文是否适用。</summary>
    bool CanDetect(AnomalyContext context);

    /// <summary>检测异常。</summary>
    Task<IReadOnlyList<AnomalyFinding>> DetectAsync(
        AnomalyContext context,
        CancellationToken cancellationToken = default);
}
