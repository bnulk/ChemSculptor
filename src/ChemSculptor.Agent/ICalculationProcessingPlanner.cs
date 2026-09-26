using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 通用计算处理方案生成器。
/// 只处理通用计算结果，不包含任何计算程序专用规则。
/// </summary>
public interface ICalculationProcessingPlanner
{
    /// <summary>根据通用计算结果生成通用处理方案。</summary>
    CalculationProcessingPlan CreatePlan(
        CalculationJob job,
        CalculationResult result);
}
