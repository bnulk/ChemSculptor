using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Models;

/// <summary>派生恢复作业的执行结果。</summary>
public sealed class RecoveryJobExecutionResult
{
    /// <summary>是否实际提交了派生作业。</summary>
    public bool Attempted { get; set; }

    /// <summary>派生作业是否正常结束并成功解析结果。</summary>
    public bool Succeeded { get; set; }

    /// <summary>采用的通用修正意图代码。</summary>
    public string CorrectionIntentCode { get; set; } = string.Empty;

    /// <summary>面向过程的执行说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>执行后的派生作业。</summary>
    public CalculationJob? RecoveryJob { get; set; }

    /// <summary>派生单点计算解析出的通用结果。</summary>
    public CalculationResult? Result { get; set; }
}
