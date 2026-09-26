using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 处理方案翻译测试。</summary>
public class GaussianProcessingPlanTranslatorTests
{
    /// <summary>验证通用异常处理动作会翻译为 Gaussian 专用动作。</summary>
    [Fact]
    public void TranslatesGenericRetryActionToGaussianAction()
    {
        CalculationProcessingPlan sourcePlan = new CalculationProcessingPlan();
        sourcePlan.JobId = "job-plan-test";
        sourcePlan.Outcome = CalculationProcessingOutcome.NeedsRetry;
        sourcePlan.Summary = "需要重试";

        CalculationProcessingAction sourceAction = new CalculationProcessingAction();
        sourceAction.Code = "calculation.retry_as_is";
        sourceAction.ActionType = CalculationProcessingActionType.RetryAsIs;
        sourceAction.Title = "重新计算";
        sourceAction.Description = "使用当前输入重新计算。";
        sourceAction.RequiresApproval = true;
        sourceAction.Parameters["failureKind"] =
            CalculationFailureKind.EnergyMissing.ToString();
        sourcePlan.Actions.Add(sourceAction);

        GaussianProcessingPlanTranslator translator =
            new GaussianProcessingPlanTranslator();

        GaussianProcessingPlan gaussianPlan = translator.Translate(sourcePlan);
        ProgramProcessingPlan programPlan =
            translator.ToProgramPlan(gaussianPlan);

        Assert.Single(gaussianPlan.Actions);
        Assert.Equal(
            GaussianProcessingActionKind.RerunSameInput,
            gaussianPlan.Actions[0].Kind);

        Assert.Single(programPlan.Actions);
        Assert.Equal("RerunSameInput", programPlan.Actions[0].Kind);
        Assert.Equal("Gaussian 16", programPlan.Program);
    }

    /// <summary>验证正常完成时不会产生处理动作。</summary>
    [Fact]
    public void CompletedPlanHasNoActions()
    {
        CalculationProcessingPlan sourcePlan = new CalculationProcessingPlan();
        sourcePlan.JobId = "job-plan-test";
        sourcePlan.Outcome = CalculationProcessingOutcome.Completed;
        sourcePlan.Summary = "正常完成";

        GaussianProcessingPlanTranslator translator =
            new GaussianProcessingPlanTranslator();

        GaussianProcessingPlan gaussianPlan = translator.Translate(sourcePlan);
        ProgramProcessingPlan programPlan =
            translator.ToProgramPlan(gaussianPlan);

        Assert.Empty(gaussianPlan.Actions);
        Assert.Empty(programPlan.Actions);
    }
}
