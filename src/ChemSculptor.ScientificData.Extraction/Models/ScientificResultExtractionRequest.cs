using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;

namespace ChemSculptor.ScientificData.Extraction.Models;

/// <summary>从一次计算和恢复过程提取科学成果的请求。</summary>
public sealed class ScientificResultExtractionRequest
{
    /// <summary>根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>客户端原始坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>原始计算作业。</summary>
    public CalculationJob OriginalJob { get; set; } = new CalculationJob();

    /// <summary>原始计算结果。</summary>
    public CalculationResult OriginalResult { get; set; } = new CalculationResult();

    /// <summary>原始计算结果验证报告。</summary>
    public CalculationValidationReport OriginalValidationReport { get; set; } =
        new CalculationValidationReport();

    /// <summary>原始波函数稳定性检查结果。</summary>
    public AnomalyCheckResult OriginalStabilityCheck { get; set; } =
        new AnomalyCheckResult();

    /// <summary>修正规划结果。</summary>
    public WavefunctionStabilityCorrectionPlanningResult CorrectionPlanning
    {
        get;
        set;
    } = new WavefunctionStabilityCorrectionPlanningResult();

    /// <summary>派生恢复作业执行结果；没有修正时为空。</summary>
    public RecoveryJobExecutionResult? RecoveryExecution { get; set; }

    /// <summary>派生恢复作业的稳定性复检结果；没有复检时为空。</summary>
    public AnomalyCheckResult? RecoveryStabilityCheck { get; set; }
}
