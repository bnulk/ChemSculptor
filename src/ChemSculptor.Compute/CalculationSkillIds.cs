namespace ChemSculptor.Compute;

/// <summary>计算工作流使用的技能标识。</summary>
public static class CalculationSkillIds
{
    /// <summary>完成一次稳定的单点计算。</summary>
    public const string CalculationSinglePoint =
        "calculation.single-point";

    /// <summary>完成一次正常的几何优化计算。</summary>
    public const string CalculationOptimization =
        "calculation.optimization";

    /// <summary>通用计算结果验证。</summary>
    public const string CalculationResultValidation =
        "calculation.result-validation";

    /// <summary>提交计算作业。</summary>
    public const string CalculationSubmission =
        "calculation.submit";

    /// <summary>等待计算作业结束。</summary>
    public const string CalculationWait =
        "calculation.wait";

    /// <summary>工作流中的结果验证。</summary>
    public const string CalculationWorkflowValidation =
        "calculation.workflow-validation";

    /// <summary>工作流中的处理方案生成。</summary>
    public const string CalculationWorkflowProcessingPlan =
        "calculation.workflow-processing-plan";

    /// <summary>准备计算程序输入。</summary>
    public const string CalculationInputPreparation =
        "calculation.prepare-input";

    /// <summary>提取计算程序结果。</summary>
    public const string CalculationResultExtraction =
        "calculation.extract-result";

    /// <summary>验证几何优化结果。</summary>
    public const string CalculationOptimizationValidation =
        "calculation.validate-optimization";

    /// <summary>生成优化任务处理方案。</summary>
    public const string CalculationOptimizationProcessingPlan =
        "calculation.optimization-processing-plan";

    /// <summary>对最终优化结构执行振动分析。</summary>
    public const string CalculationFrequencyAnalysis =
        "calculation.frequency-analysis";

    /// <summary>完成一次 TDDFT 单点计算。</summary>
    public const string CalculationTddftSinglePoint =
        "calculation.tddft-single-point";
}
