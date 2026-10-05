using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Models;

/// <summary>创建派生恢复作业的结果。</summary>
public sealed class RecoveryJobPreparationResult
{
    /// <summary>是否成功创建。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>派生恢复作业。</summary>
    public CalculationJob? RecoveryJob { get; set; }

    /// <summary>恢复尝试记录。</summary>
    public RecoveryAttempt? Attempt { get; set; }
}
