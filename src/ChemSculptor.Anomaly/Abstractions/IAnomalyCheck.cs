using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>统一的异常检查契约。</summary>
public interface IAnomalyCheck
{
    /// <summary>检查描述。</summary>
    AnomalyCheckDescriptor Descriptor { get; }

    /// <summary>判断当前上下文是否适用。</summary>
    bool CanCheck(AnomalyContext context);

    /// <summary>执行检查并返回统一结果。</summary>
    Task<AnomalyCheckResult> CheckAsync(
        AnomalyContext context,
        CancellationToken cancellationToken = default);
}
