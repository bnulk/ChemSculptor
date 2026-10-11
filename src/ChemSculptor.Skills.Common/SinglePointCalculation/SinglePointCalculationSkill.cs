using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.SinglePointCalculation;

/// <summary>
/// 完成一次稳定单点计算的复合 Skill。
/// 它只负责正常计算路径，异常诊断、修正和重算由外层工作流负责。
/// </summary>
public sealed class SinglePointCalculationSkill
    : JsonSkill<
        CalculationInputGenerationRequest,
        SinglePointCalculationSkillResult>
{
    private readonly ISkillRegistry _skills;
    private readonly List<string> _capabilities;

    /// <summary>创建单点计算 Skill。</summary>
    public SinglePointCalculationSkill(ISkillRegistry skills)
    {
        if (skills == null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        _skills = skills;
        _capabilities = new List<string>();
        _capabilities.Add(CalculationSkillIds.CalculationSinglePoint);
        _capabilities.Add("calculation.result");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return CalculationSkillIds.CalculationSinglePoint; }
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

    /// <summary>执行一次单点计算的正常路径。</summary>
    protected override async Task<SinglePointCalculationSkillResult>
        ExecuteAsync(
            CalculationInputGenerationRequest request,
            CancellationToken cancellationToken)
    {
        SinglePointCalculationSkillResult result =
            new SinglePointCalculationSkillResult();

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
            DeserializeStepResult<CalculationResultExtractionResult>(
                extractionTask,
                "结果提取");
        AddStep(
            result,
            "extract-result",
            CalculationSkillIds.CalculationResultExtraction,
            extractionTask,
            extractionStartedAt);

        if (!extractionResult.Succeeded)
        {
            throw new InvalidOperationException(
                BuildStepError("结果提取", extractionResult.Error));
        }

        DateTimeOffset validationStartedAt =
            DateTimeOffset.UtcNow;
        TaskResult validationTask = await InvokeStepAsync(
            request,
            "validate",
            CalculationSkillIds.CalculationWorkflowValidation,
            "wait",
            waitTask.Output!,
            "extraction",
            extractionTask.Output!,
            cancellationToken);
        CalculationWorkflowValidationSkillResult validationResult =
            DeserializeStepResult<
                CalculationWorkflowValidationSkillResult>(
                    validationTask,
                    "结果验证");
        AddStep(
            result,
            "validate",
            CalculationSkillIds.CalculationWorkflowValidation,
            validationTask,
            validationStartedAt);

        result.Succeeded = extractionResult.Succeeded;
        result.Passed = validationResult.Passed;
        result.Job = validationResult.Job;
        result.Result = extractionResult.Result;
        result.Report = validationResult.Report;
        result.Wait = waitResult;

        if (!result.Passed)
        {
            result.Error = string.IsNullOrWhiteSpace(
                validationResult.Report.Summary)
                ? "单点计算验证未通过。"
                : validationResult.Report.Summary;
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
                CalculationSkillIds.CalculationResultExtraction) != null
            && _skills.Resolve(
                CalculationSkillIds.CalculationWorkflowValidation) != null;

        return Task.FromResult(healthy);
    }

    /// <summary>调用一个原子 Skill。</summary>
    private async Task<TaskResult> InvokeStepAsync(
        CalculationInputGenerationRequest request,
        string stepName,
        string skillId,
        string inputName,
        string inputValue,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> inputs =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        inputs[inputName] = inputValue;
        return await InvokeStepAsync(
            request,
            stepName,
            skillId,
            inputs,
            cancellationToken);
    }

    /// <summary>调用一个需要两个输入的原子 Skill。</summary>
    private async Task<TaskResult> InvokeStepAsync(
        CalculationInputGenerationRequest request,
        string stepName,
        string skillId,
        string firstInputName,
        string firstInputValue,
        string secondInputName,
        string secondInputValue,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> inputs =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        inputs[firstInputName] = firstInputValue;
        inputs[secondInputName] = secondInputValue;
        return await InvokeStepAsync(
            request,
            stepName,
            skillId,
            inputs,
            cancellationToken);
    }

    /// <summary>调用一个原子 Skill。</summary>
    private async Task<TaskResult> InvokeStepAsync(
        CalculationInputGenerationRequest request,
        string stepName,
        string skillId,
        Dictionary<string, string> inputs,
        CancellationToken cancellationToken)
    {
        ISkill? skill = _skills.Resolve(skillId);

        if (skill == null)
        {
            throw new InvalidOperationException(
                "单点计算 Skill 缺少原子 Skill：" + skillId);
        }

        TaskRequest stepRequest = new TaskRequest();
        stepRequest.WorkflowId =
            string.IsNullOrWhiteSpace(request.Job.WorkflowId)
                ? request.Job.JobId
                : request.Job.WorkflowId;
        stepRequest.NodeId = stepName;
        stepRequest.SkillId = skillId;
        stepRequest.Inputs = inputs;

        return await skill.ExecuteAsync(
            stepRequest,
            cancellationToken);
    }

    /// <summary>读取原子 Skill 的结果。</summary>
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

    /// <summary>记录一个执行步骤。</summary>
    private static void AddStep(
        SinglePointCalculationSkillResult result,
        string stepName,
        string skillId,
        TaskResult taskResult,
        DateTimeOffset startedAt)
    {
        SinglePointCalculationStepResult step =
            new SinglePointCalculationStepResult();
        step.Name = stepName;
        step.SkillId = skillId;
        step.Succeeded = taskResult.Succeeded;
        step.Diagnostics = taskResult.Diagnostics ?? string.Empty;
        step.StartedAt = startedAt;
        step.CompletedAt = taskResult.CompletedAt;
        result.Steps.Add(step);
    }

    /// <summary>构造步骤失败说明。</summary>
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
