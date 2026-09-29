using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>
/// 通用计算结果验证技能。
/// 当前验证正常结束、最终能量和通用失败类别。
/// </summary>
public sealed class CalculationResultValidationSkill
    : JsonSkill<
        CalculationResultValidationSkillRequest,
        CalculationResultValidationSkillResult>
{
    private readonly CalculationValidationService _validationService;
    private readonly List<string> _capabilities;

    /// <summary>创建验证技能。</summary>
    public CalculationResultValidationSkill(
        CalculationValidationService validationService)
    {
        if (validationService == null)
        {
            throw new ArgumentNullException(nameof(validationService));
        }

        _validationService = validationService;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.validation");
        _capabilities.Add("calculation.result");
        _capabilities.Add("calculation.single-point-validation");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return CalculationResultValidationSkillDescriptor.Id; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return CalculationResultValidationSkillDescriptor.VersionValue; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>执行验证。</summary>
    protected override Task<CalculationResultValidationSkillResult> ExecuteAsync(
        CalculationResultValidationSkillRequest request,
        CancellationToken cancellationToken)
    {
        CalculationResultValidationResult validationResult =
            _validationService.Validate(request);

        CalculationResultValidationSkillResult result =
            new CalculationResultValidationSkillResult();
        result.Passed = validationResult.Passed;
        result.Report = validationResult.Report;
        return Task.FromResult(result);
    }

    /// <summary>验证技能始终可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

}
