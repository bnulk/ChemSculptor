using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationCalculation;

/// <summary>
/// 完成一次正常几何优化计算的复合 Skill。
/// 它只执行一个优化作业，并在一次输出中解析完整轨迹。
/// </summary>
public sealed class OptimizationCalculationSkill
    : JsonSkill<
        CalculationInputGenerationRequest,
        OptimizationCalculationSkillResult>
{
    private readonly ISkillRegistry _skills;
    private readonly List<string> _capabilities;

    /// <summary>创建几何优化 Skill。</summary>
    public OptimizationCalculationSkill(ISkillRegistry skills)
    {
        if (skills == null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        _skills = skills;
        _capabilities = new List<string>();
        _capabilities.Add(CalculationSkillIds.CalculationOptimization);
        _capabilities.Add("calculation.optimization-result");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return CalculationSkillIds.CalculationOptimization; }
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

    /// <summary>执行一次正常几何优化计算。</summary>
    protected override async Task<OptimizationCalculationSkillResult>
        ExecuteAsync(
            CalculationInputGenerationRequest request,
            CancellationToken cancellationToken)
    {
        if (request.Spec.TaskType != CalculationTaskType.Optimization)
        {
            throw new InvalidOperationException(
                "几何优化 Skill 只接受 Optimization 任务。");
        }

        request.Job.Spec = request.Spec;
        OptimizationCalculationSkillResult result =
            new OptimizationCalculationSkillResult();

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
                "提交优化");
        AddStep(
            result,
            "submit",
            CalculationSkillIds.CalculationSubmission,
            submitTask,
            submitStartedAt);

        if (!submissionResult.Succeeded)
        {
            throw new InvalidOperationException(
                BuildStepError("提交优化", submissionResult.Error));
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
                "等待优化");
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
                    "优化结果提取");
        AddStep(
            result,
            "extract-result",
            CalculationSkillIds.CalculationResultExtraction,
            extractionTask,
            extractionStartedAt);

        if (!extractionResult.Succeeded
            || extractionResult.OptimizationResult == null)
        {
            throw new InvalidOperationException(
                BuildStepError(
                    "优化结果提取",
                    extractionResult.Error));
        }

        DateTimeOffset validationStartedAt =
            DateTimeOffset.UtcNow;
        TaskResult validationTask = await InvokeStepAsync(
            request,
            "validate-optimization",
            CalculationSkillIds.CalculationOptimizationValidation,
            "wait",
            waitTask.Output!,
            "extraction",
            extractionTask.Output!,
            cancellationToken);
        OptimizationValidationSkillResult validationResult =
            DeserializeStepResult<
                OptimizationValidationSkillResult>(
                    validationTask,
                    "优化结果验证");
        AddStep(
            result,
            "validate-optimization",
            CalculationSkillIds.CalculationOptimizationValidation,
            validationTask,
            validationStartedAt);

        result.Succeeded = extractionResult.Succeeded;
        result.Passed = validationResult.Passed;
        result.Job = validationResult.Job;
        result.Result = validationResult.Result;
        result.Report = validationResult.Report;
        result.Wait = waitResult;

        if (!result.Passed)
        {
            result.Error = string.IsNullOrWhiteSpace(
                validationResult.Report.Summary)
                ? "几何优化验证未通过。"
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
                CalculationSkillIds
                    .CalculationOptimizationValidation) != null;

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
                "几何优化 Skill 缺少原子 Skill：" + skillId);
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
        OptimizationCalculationSkillResult result,
        string stepName,
        string skillId,
        TaskResult taskResult,
        DateTimeOffset startedAt)
    {
        OptimizationCalculationStepResult step =
            new OptimizationCalculationStepResult();
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
