namespace ChemSculptor.Anomaly.Models;

/// <summary>波函数稳定性修正方案生成结果。</summary>
public sealed class WavefunctionStabilityCorrectionPlanningResult
{
    /// <summary>是否生成了修正方案。</summary>
    public bool PlanCreated { get; set; }

    /// <summary>生成的修正计划。</summary>
    public CorrectionPlan? Plan { get; set; }

    /// <summary>关联的异常处理记录标识。</summary>
    public string AnomalyRecordId { get; set; } = string.Empty;

    /// <summary>结果说明。</summary>
    public string Message { get; set; } = string.Empty;
}
