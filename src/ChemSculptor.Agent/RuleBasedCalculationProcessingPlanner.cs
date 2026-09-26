using ChemSculptor.Compute;

namespace ChemSculptor.Agent;

/// <summary>
/// 基于通用结果的规则处理方案生成器。
/// 当前只区分正常完成、缺少能量和需要人工检查等基本场景。
/// </summary>
public sealed class RuleBasedCalculationProcessingPlanner : ICalculationProcessingPlanner
{
    /// <summary>生成通用处理方案。</summary>
    public CalculationProcessingPlan CreatePlan(
        CalculationJob job,
        CalculationResult result)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (result == null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        CalculationProcessingPlan plan = new CalculationProcessingPlan();
        plan.JobId = job.JobId;

        if (job.State == CalculationJobState.Completed
            && result.NormalTermination
            && result.Energy.HasValue)
        {
            plan.Outcome = CalculationProcessingOutcome.Completed;
            plan.Summary = "计算正常结束，最终能量已成功提取。";

            if (HasWarning(result))
            {
                plan.Outcome = CalculationProcessingOutcome.CompletedWithWarnings;
                plan.Summary = "计算正常结束，但输出中存在需要关注的警告。";
            }

            return plan;
        }

        if (result.FailureKind == CalculationFailureKind.EnergyMissing)
        {
            plan.Outcome = CalculationProcessingOutcome.NeedsRetry;
            plan.Summary = "计算已结束，但没有提取到最终能量。";
            AddAction(
                plan,
                "calculation.retry_as_is",
                CalculationProcessingActionType.RetryAsIs,
                "重新计算",
                "使用当前输入和设置重新计算。",
                true,
                result.FailureKind);
            return plan;
        }

        plan.Outcome = CalculationProcessingOutcome.NeedsUserInput;
        plan.Summary = "计算没有正常完成，需要人工检查输出。";
        AddAction(
            plan,
            "calculation.review_output",
            CalculationProcessingActionType.ReviewOutput,
            "检查输出",
            "检查程序输出和诊断信息，确认下一步处理方式。",
            true,
            result.FailureKind);

        return plan;
    }

    private static bool HasWarning(CalculationResult result)
    {
        for (int index = 0; index < result.Diagnostics.Count; index++)
        {
            if (result.Diagnostics[index].Severity == CalculationDiagnosticSeverity.Warning)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddAction(
        CalculationProcessingPlan plan,
        string code,
        CalculationProcessingActionType actionType,
        string title,
        string description,
        bool requiresApproval,
        CalculationFailureKind failureKind)
    {
        CalculationProcessingAction action = new CalculationProcessingAction();
        action.Code = code;
        action.ActionType = actionType;
        action.Title = title;
        action.Description = description;
        action.RequiresApproval = requiresApproval;
        action.Parameters["failureKind"] = failureKind.ToString();
        plan.Actions.Add(action);
    }
}
