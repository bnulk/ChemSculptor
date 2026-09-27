using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>计算结果验证策略。</summary>
public interface ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    string Name { get; }

    /// <summary>判断当前验证器是否可以处理该请求。</summary>
    bool CanValidate(CalculationResultValidationRequest request);

    /// <summary>执行验证。</summary>
    CalculationValidationReport Validate(
        CalculationResultValidationRequest request);
}
