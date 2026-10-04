using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Abstractions;

/// <summary>恢复结果验证器。</summary>
public interface IRecoveryValidator
{
    /// <summary>能够处理的异常代码。</summary>
    string Code { get; }

    /// <summary>验证器版本。</summary>
    string Version { get; }

    /// <summary>判断是否适用于本次恢复。</summary>
    bool CanValidate(
        AnomalyFinding finding,
        RecoveryAttempt attempt);

    /// <summary>验证恢复结果。</summary>
    Task<RecoveryValidationResult> ValidateAsync(
        AnomalyContext originalContext,
        AnomalyContext recoveryContext,
        AnomalyFinding finding,
        RecoveryAttempt attempt,
        CancellationToken cancellationToken = default);
}
