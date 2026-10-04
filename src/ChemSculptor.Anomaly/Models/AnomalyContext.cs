using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Models;

/// <summary>
/// 异常检测、诊断和恢复判断所需的上下文。
/// </summary>
public sealed class AnomalyContext
{
    /// <summary>原始计算作业。</summary>
    public CalculationJob? Job { get; set; }

    /// <summary>通用计算结果。</summary>
    public CalculationResult? Result { get; set; }

    /// <summary>通用结果验证报告。</summary>
    public CalculationValidationReport? Validation { get; set; }

    /// <summary>与本次异常处理有关的附加数据。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
