namespace ChemSculptor.Anomaly.Models;

/// <summary>异常处理记录的总体状态。</summary>
public enum AnomalyRecordStatus
{
    /// <summary>已创建，尚未完成检查。</summary>
    Open,

    /// <summary>已完成检查，没有发现异常。</summary>
    Passed,

    /// <summary>已完成诊断。</summary>
    Diagnosed,

    /// <summary>正在等待审批。</summary>
    AwaitingApproval,

    /// <summary>方案已获预授权，可以进入恢复执行。</summary>
    ReadyForRecovery,

    /// <summary>正在执行恢复作业。</summary>
    Recovering,

    /// <summary>异常已解决。</summary>
    Resolved,

    /// <summary>处理失败或无法安全恢复。</summary>
    Failed,

    /// <summary>已关闭。</summary>
    Closed
}
