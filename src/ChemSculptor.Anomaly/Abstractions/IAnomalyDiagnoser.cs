using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>异常诊断器。</summary>
public interface IAnomalyDiagnoser
{
    /// <summary>能够处理的异常代码。</summary>
    string Code { get; }

    /// <summary>诊断器版本。</summary>
    string Version { get; }

    /// <summary>判断是否能够诊断指定异常。</summary>
    bool CanDiagnose(AnomalyFinding finding);

    /// <summary>诊断异常。</summary>
    Task<DiagnosisReport> DiagnoseAsync(
        AnomalyContext context,
        AnomalyFinding finding,
        CancellationToken cancellationToken = default);
}
