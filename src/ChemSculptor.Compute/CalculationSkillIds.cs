namespace ChemSculptor.Compute;

/// <summary>计算工作流使用的技能标识。</summary>
public static class CalculationSkillIds
{
    /// <summary>Gaussian 输入文件生成。</summary>
    public const string GaussianInputGeneration =
        "gaussian.input-generation";

    /// <summary>Gaussian 单点计算结果提取。</summary>
    public const string GaussianSinglePointResultExtraction =
        "gaussian.single-point-result-extraction";

    /// <summary>Gaussian 异常诊断。</summary>
    public const string GaussianFailureDiagnosis =
        "gaussian.failure-diagnosis";

    /// <summary>Gaussian 异常修正提案。</summary>
    public const string GaussianFailureCorrectionProposal =
        "gaussian.failure-correction-proposal";

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

    /// <summary>工作流中的 Gaussian 单点结果提取。</summary>
    public const string GaussianSinglePointWorkflowExtraction =
        "gaussian.single-point-workflow-extraction";
}
