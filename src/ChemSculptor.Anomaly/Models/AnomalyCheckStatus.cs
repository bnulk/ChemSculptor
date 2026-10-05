namespace ChemSculptor.Anomaly.Models;

/// <summary>异常检查的执行结果状态。</summary>
public enum AnomalyCheckStatus
{
    /// <summary>尚未执行。</summary>
    NotRun,

    /// <summary>检查通过，没有发现异常。</summary>
    Passed,

    /// <summary>检查完成并发现异常。</summary>
    Finding,

    /// <summary>检查不适用于当前任务或计算程序。</summary>
    Skipped,

    /// <summary>检查本身执行失败。</summary>
    ExecutionFailed,

    /// <summary>检查已取消。</summary>
    Canceled
}
