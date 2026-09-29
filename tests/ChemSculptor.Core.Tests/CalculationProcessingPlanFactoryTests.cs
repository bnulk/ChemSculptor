using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>通用计算处理方案工厂测试。</summary>
public class CalculationProcessingPlanFactoryTests
{
    /// <summary>验证正常能量结果生成完成方案。</summary>
    [Fact]
    public void CreatesCompletedPlanForNormalResult()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-plan";
        job.State = CalculationJobState.Validated;

        CalculationResult result = new CalculationResult();
        result.NormalTermination = true;
        result.Energy = -76.0;
        result.FailureKind = CalculationFailureKind.None;

        CalculationProcessingPlan plan =
            CalculationProcessingPlanFactory.Create(job, result);

        Assert.Equal(CalculationProcessingOutcome.Completed, plan.Outcome);
        Assert.Empty(plan.Actions);
    }

    /// <summary>验证缺少能量会生成通用重试方案。</summary>
    [Fact]
    public void CreatesRetryPlanWhenEnergyIsMissing()
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-plan";
        job.State = CalculationJobState.Completed;

        CalculationResult result = new CalculationResult();
        result.NormalTermination = true;
        result.FailureKind = CalculationFailureKind.EnergyMissing;

        CalculationProcessingPlan plan =
            CalculationProcessingPlanFactory.Create(job, result);

        Assert.Equal(CalculationProcessingOutcome.NeedsRetry, plan.Outcome);
        Assert.Single(plan.Actions);
        Assert.Equal(
            CalculationProcessingActionType.RetryAsIs,
            plan.Actions[0].ActionType);
    }
}
