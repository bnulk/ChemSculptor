using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 处理方案翻译器。
/// 把智能体生成的通用处理方案翻译为 Gaussian 专用处理方案。
/// </summary>
public sealed class GaussianProcessingPlanTranslator
{
    /// <summary>翻译为 Gaussian 专用处理方案。</summary>
    public GaussianProcessingPlan Translate(CalculationProcessingPlan processingPlan)
    {
        if (processingPlan == null)
        {
            throw new ArgumentNullException(nameof(processingPlan));
        }

        GaussianProcessingPlan gaussianPlan = new GaussianProcessingPlan();
        gaussianPlan.JobId = processingPlan.JobId;
        gaussianPlan.Summary = processingPlan.Summary;
        gaussianPlan.CreatedAt = processingPlan.CreatedAt;

        for (int index = 0; index < processingPlan.Actions.Count; index++)
        {
            CalculationProcessingAction sourceAction = processingPlan.Actions[index];
            GaussianProcessingAction targetAction = new GaussianProcessingAction();
            targetAction.Code = sourceAction.Code;
            targetAction.Kind = TranslateActionType(sourceAction.ActionType);
            targetAction.Description = sourceAction.Description;
            targetAction.RequiresApproval = sourceAction.RequiresApproval;
            targetAction.Parameters = CopyParameters(sourceAction.Parameters);
            gaussianPlan.Actions.Add(targetAction);
        }

        return gaussianPlan;
    }

    /// <summary>
    /// 把 Gaussian 专用方案映射为适配器返回的通用程序方案。
    /// Agent 只接触这一层，不直接依赖 GaussianProcessingPlan。
    /// </summary>
    public ProgramProcessingPlan ToProgramPlan(GaussianProcessingPlan gaussianPlan)
    {
        if (gaussianPlan == null)
        {
            throw new ArgumentNullException(nameof(gaussianPlan));
        }

        ProgramProcessingPlan programPlan = new ProgramProcessingPlan();
        programPlan.JobId = gaussianPlan.JobId;
        programPlan.Program = Gaussian16ProgramAdapter.ProgramNameValue;
        programPlan.Summary = gaussianPlan.Summary;
        programPlan.CreatedAt = gaussianPlan.CreatedAt;

        for (int index = 0; index < gaussianPlan.Actions.Count; index++)
        {
            GaussianProcessingAction sourceAction = gaussianPlan.Actions[index];
            ProgramProcessingAction targetAction = new ProgramProcessingAction();
            targetAction.Code = sourceAction.Code;
            targetAction.Kind = sourceAction.Kind.ToString();
            targetAction.Description = sourceAction.Description;
            targetAction.RequiresApproval = sourceAction.RequiresApproval;
            targetAction.Parameters = CopyParameters(sourceAction.Parameters);
            programPlan.Actions.Add(targetAction);
        }

        return programPlan;
    }

    private static GaussianProcessingActionKind TranslateActionType(
        CalculationProcessingActionType actionType)
    {
        if (actionType == CalculationProcessingActionType.ReviewOutput)
        {
            return GaussianProcessingActionKind.InspectOutput;
        }

        if (actionType == CalculationProcessingActionType.RetryAsIs)
        {
            return GaussianProcessingActionKind.RerunSameInput;
        }

        if (actionType == CalculationProcessingActionType.RetryWithChanges)
        {
            return GaussianProcessingActionKind.ModifyInput;
        }

        if (actionType == CalculationProcessingActionType.RequestUserInput)
        {
            return GaussianProcessingActionKind.RequestUserDecision;
        }

        if (actionType == CalculationProcessingActionType.Abort)
        {
            return GaussianProcessingActionKind.Abort;
        }

        return GaussianProcessingActionKind.None;
    }

    private static Dictionary<string, string> CopyParameters(
        Dictionary<string, string> source)
    {
        Dictionary<string, string> target =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (KeyValuePair<string, string> pair in source)
        {
            target[pair.Key] = pair.Value;
        }

        return target;
    }
}
