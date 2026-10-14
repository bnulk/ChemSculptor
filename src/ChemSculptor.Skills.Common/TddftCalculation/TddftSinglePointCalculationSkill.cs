using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.TddftCalculation;

/// <summary>
/// 完成一次正常 TDDFT 单点计算的复合 Skill。
/// 暂时不包含异常恢复和完整 Workflow。
/// </summary>
public sealed class TddftSinglePointCalculationSkill
    : JsonSkill<
        CalculationInputGenerationRequest,
        TddftSinglePointCalculationSkillResult>
{
    private readonly ISkillRegistry _skills;
    private readonly List<string> _capabilities;

    /// <summary>创建 TDDFT 单点计算 Skill。</summary>
    public TddftSinglePointCalculationSkill(
        ISkillRegistry skills)
    {
        if (skills == null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        _skills = skills;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationTddftSinglePoint);
        _capabilities.Add("calculation.tddft-result");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationTddftSinglePoint;
        }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>执行一次 TDDFT 单点计算。</summary>
    protected override async Task<
        TddftSinglePointCalculationSkillResult>
        ExecuteAsync(
            CalculationInputGenerationRequest request,
            CancellationToken cancellationToken)
    {
        if (request.Spec.TaskType != CalculationTaskType.TdDft)
        {
            throw new InvalidOperationException(
                "TDDFT Skill 只接受 TdDft 任务。");
        }

        request.Job.Spec = request.Spec;
        TddftSinglePointCalculationSkillResult result =
            new TddftSinglePointCalculationSkillResult();

        DateTimeOffset inputStartedAt = DateTimeOffset.UtcNow;
        TaskResult inputTask = await InvokeStepAsync(
            request,
            "prepare-input",
            CalculationSkillIds.CalculationInputPreparation,
            "request",
            SkillJson.Serialize(request),
            cancellationToken);
        CalculationInputGenerationResult inputResult =
            DeserializeStepResult<CalculationInputGenerationResult>(
                inputTask,
                "输入准备");
        AddStep(
            result,
            "prepare-input",
            CalculationSkillIds.CalculationInputPreparation,
            inputTask,
            inputStartedAt);

        if (!inputResult.Succeeded)
        {
            throw new InvalidOperationException(
                BuildStepError("输入准备", inputResult.Error));
        }

        DateTimeOffset submitStartedAt = DateTimeOffset.UtcNow;
        TaskResult submitTask = await InvokeStepAsync(
            request,
            "submit",
            CalculationSkillIds.CalculationSubmission,
            "inputResult",
            inputTask.Output!,
            cancellationToken);
        CalculationSubmissionSkillResult submissionResult =
            DeserializeStepResult<CalculationSubmissionSkillResult>(
                submitTask,
                "提交计算");
        AddStep(
            result,
            "submit",
            CalculationSkillIds.CalculationSubmission,
            submitTask,
            submitStartedAt);

        if (!submissionResult.Succeeded)
        {
            throw new InvalidOperationException(
                BuildStepError("提交计算", submissionResult.Error));
        }

        DateTimeOffset waitStartedAt = DateTimeOffset.UtcNow;
        TaskResult waitTask = await InvokeStepAsync(
            request,
            "wait",
            CalculationSkillIds.CalculationWait,
            "submission",
            submitTask.Output!,
            cancellationToken);
        CalculationWaitSkillResult waitResult =
            DeserializeStepResult<CalculationWaitSkillResult>(
                waitTask,
                "等待计算");
        AddStep(
            result,
            "wait",
            CalculationSkillIds.CalculationWait,
            waitTask,
            waitStartedAt);

        DateTimeOffset extractionStartedAt =
            DateTimeOffset.UtcNow;
        TaskResult extractionTask = await InvokeStepAsync(
            request,
            "extract-result",
            CalculationSkillIds.CalculationResultExtraction,
            "submission",
            submitTask.Output!,
            cancellationToken);
        CalculationResultExtractionResult extractionResult =
            DeserializeStepResult<
                CalculationResultExtractionResult>(
                    extractionTask,
                    "TDDFT 结果提取");
        AddStep(
            result,
            "extract-result",
            CalculationSkillIds.CalculationResultExtraction,
            extractionTask,
            extractionStartedAt);

        if (!extractionResult.Succeeded
            || extractionResult.TddftResult == null)
        {
            throw new InvalidOperationException(
                BuildStepError(
                    "TDDFT 结果提取",
                    extractionResult.Error));
        }

        TddftCalculationResult tddftResult =
            extractionResult.TddftResult;
        result.Succeeded = true;
        result.Passed =
            tddftResult.NormalTermination
            && tddftResult.ExcitedStates.Count > 0;
        result.Job = waitResult.Job;
        result.Result = tddftResult;
        result.Wait = waitResult;

        if (!result.Passed)
        {
            result.Error =
                "TDDFT 计算没有正常结束，或没有解析到激发态。";
        }

        return result;
    }

    /// <summary>检查原子 Skill 是否已注册。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        bool healthy =
            _skills.Resolve(
                CalculationSkillIds.CalculationInputPreparation) != null
            && _skills.Resolve(
                CalculationSkillIds.CalculationSubmission) != null
            && _skills.Resolve(
                CalculationSkillIds.CalculationWait) != null
            && _skills.Resolve(
                CalculationSkillIds.CalculationResultExtraction) != null;

        return Task.FromResult(healthy);
    }

    private async Task<TaskResult> InvokeStepAsync(
        CalculationInputGenerationRequest request,
        string stepName,
        string skillId,
        string inputName,
        string inputValue,
        CancellationToken cancellationToken)
    {
        ISkill? skill = _skills.Resolve(skillId);

        if (skill == null)
        {
            throw new InvalidOperationException(
                "TDDFT Skill 缺少原子 Skill：" + skillId);
        }

        TaskRequest stepRequest = new TaskRequest();
        stepRequest.WorkflowId =
            string.IsNullOrWhiteSpace(request.Job.WorkflowId)
                ? request.Job.JobId
                : request.Job.WorkflowId;
        stepRequest.NodeId = stepName;
        stepRequest.SkillId = skillId;
        stepRequest.Inputs[inputName] = inputValue;
        return await skill.ExecuteAsync(
            stepRequest,
            cancellationToken);
    }

    private static T DeserializeStepResult<T>(
        TaskResult taskResult,
        string stepDisplayName)
    {
        if (!taskResult.Succeeded)
        {
            throw new InvalidOperationException(
                BuildStepError(
                    stepDisplayName,
                    taskResult.Diagnostics));
        }

        if (string.IsNullOrWhiteSpace(taskResult.Output))
        {
            throw new InvalidOperationException(
                stepDisplayName + "没有返回结果。");
        }

        return SkillJson.Deserialize<T>(taskResult.Output);
    }

    private static void AddStep(
        TddftSinglePointCalculationSkillResult result,
        string stepName,
        string skillId,
        TaskResult taskResult,
        DateTimeOffset startedAt)
    {
        TddftCalculationStepResult step =
            new TddftCalculationStepResult();
        step.Name = stepName;
        step.SkillId = skillId;
        step.Succeeded = taskResult.Succeeded;
        step.Diagnostics = taskResult.Diagnostics ?? string.Empty;
        step.StartedAt = startedAt;
        step.CompletedAt = taskResult.CompletedAt;
        result.Steps.Add(step);
    }

    private static string BuildStepError(
        string stepDisplayName,
        string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return stepDisplayName + "失败。";
        }

        return stepDisplayName + "失败：" + detail;
    }
}
