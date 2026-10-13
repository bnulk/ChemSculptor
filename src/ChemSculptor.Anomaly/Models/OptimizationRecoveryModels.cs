using ChemSculptor.Compute;

namespace ChemSculptor.Anomaly.Models;

/// <summary>优化异常诊断结果。</summary>
public sealed class OptimizationFailureDiagnosisResult
{
    /// <summary>源优化作业标识。</summary>
    public string SourceJobId { get; set; } = string.Empty;

    /// <summary>关联的异常处理记录标识。</summary>
    public string AnomalyRecordId { get; set; } = string.Empty;

    /// <summary>诊断摘要。</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>逐项诊断。</summary>
    public List<DiagnosisReport> Diagnoses { get; set; } =
        new List<DiagnosisReport>();
}

/// <summary>优化修正方案生成结果。</summary>
public sealed class OptimizationCorrectionPlanningResult
{
    /// <summary>是否生成了修正计划。</summary>
    public bool PlanCreated { get; set; }

    /// <summary>生成的修正计划。</summary>
    public CorrectionPlan? Plan { get; set; }

    /// <summary>关联的异常处理记录标识。</summary>
    public string AnomalyRecordId { get; set; } = string.Empty;

    /// <summary>结果说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>优化派生恢复作业的执行结果。</summary>
public sealed class OptimizationRecoveryExecutionResult
{
    /// <summary>是否实际提交了派生作业。</summary>
    public bool Attempted { get; set; }

    /// <summary>派生作业是否正常结束并成功解析优化结果。</summary>
    public bool Succeeded { get; set; }

    /// <summary>采用的修正意图代码。</summary>
    public string CorrectionIntentCode { get; set; } = string.Empty;

    /// <summary>面向过程的执行说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>执行后的派生作业。</summary>
    public CalculationJob? RecoveryJob { get; set; }

    /// <summary>派生作业解析出的优化结果。</summary>
    public OptimizationResult? Result { get; set; }
}
