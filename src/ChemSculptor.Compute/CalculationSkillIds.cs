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
}
