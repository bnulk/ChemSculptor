using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 吸收光谱恢复框架的默认实现。
/// 当前不自动恢复，只保留稳定扩展点，便于后续按实际异常补充策略。
/// </summary>
public sealed class NoOpAbsorptionSpectrumRecoveryFramework
    : IAbsorptionSpectrumRecoveryFramework
{
    /// <summary>生成未实现恢复计划。</summary>
    public Task<AbsorptionSpectrumRecoveryPlan> PlanAsync(
        AbsorptionSpectrumRecoveryContext context,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        AbsorptionSpectrumRecoveryPlan plan =
            new AbsorptionSpectrumRecoveryPlan();
        plan.PlanCreated = false;
        plan.StrategyId =
            AbsorptionSpectrumAnomalyCodes.RecoveryNotImplemented;
        plan.Description =
            "当前阶段保留吸收光谱恢复框架，尚未注册自动恢复策略。";
        plan.CanRetry = false;
        plan.Parameters["stage"] = context.Stage.ToString();
        plan.Parameters["failureCode"] = context.FailureCode;
        return Task.FromResult(plan);
    }
}
