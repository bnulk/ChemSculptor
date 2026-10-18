using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationFrequencyCalculation;

/// <summary>
/// 完成一次正常几何优化与振动分析的复合 Skill。
/// 只提交一个 opt freq 作业，并从同一输出中提取两类结果。
/// </summary>
public sealed class OptimizationFrequencyCalculationSkill
    : JsonSkill<
        CalculationInputGenerationRequest,
        OptimizationFrequencyCalculationSkillResult>
{
    private readonly ISkillRegistry _skills;
    private readonly List<string> _capabilities;

    /// <summary>创建联合优化频率 Skill。</summary>
    public OptimizationFrequencyCalculationSkill(
        ISkillRegistry skills)
    {
        if (skills == null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        _skills = skills;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationOptimizationFrequency);
        _capabilities.Add(
            "calculation.optimization-frequency-result");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationOptimizationFrequency;
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

    /// <summary>执行一次联合优化频率作业。</summary>
    protected override async Task<
        OptimizationFrequencyCalculationSkillResult>
        ExecuteAsync(
            CalculationInputGenerationRequest request,
            CancellationToken cancellationToken)
    {
        if (request.Spec.TaskType
            != CalculationTaskType.OptimizationFrequency)
        {
            throw new InvalidOperationException(
                "联合优化频率 Skill 只接受 OptimizationFrequency 任务。");
        }

        request.Job.Spec = request.Spec;
        OptimizationFrequencyCalculationSkillResult result =
            new OptimizationFrequencyCalculationSkillResult();

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
                "提交联合计算");
        AddStep(
            result,
            "submit",
            CalculationSkillIds.CalculationSubmission,
            submitTask,
            submitStartedAt);

        if (!submissionResult.Succeeded)
        {
            throw new InvalidOperationException(
                BuildStepError(
                    "提交联合计算",
                    submissionResult.Error));
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
                "等待联合计算");
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
                    "联合结果提取");
        AddStep(
            result,
            "extract-result",
            CalculationSkillIds.CalculationResultExtraction,
            extractionTask,
            extractionStartedAt);

        if (!extractionResult.Succeeded
            || extractionResult.OptimizationResult == null
            || extractionResult.FrequencyResult == null)
        {
            throw new InvalidOperationException(
                BuildStepError(
                    "联合结果提取",
                    extractionResult.Error));
        }

        DateTimeOffset validationStartedAt =
            DateTimeOffset.UtcNow;
        TaskResult validationTask = await InvokeTwoInputsAsync(
            request,
            "validate-optimization-frequency",
            CalculationSkillIds
                .CalculationOptimizationFrequencyValidation,
            "wait",
            waitTask.Output!,
            "extraction",
            extractionTask.Output!,
            cancellationToken);
        OptimizationFrequencyValidationSkillResult validationResult =
            DeserializeStepResult<
                OptimizationFrequencyValidationSkillResult>(
                    validationTask,
                    "联合结果验证");
        AddStep(
            result,
            "validate-optimization-frequency",
            CalculationSkillIds
                .CalculationOptimizationFrequencyValidation,
            validationTask,
            validationStartedAt);

        result.Succeeded = extractionResult.Succeeded;
        result.Passed = validationResult.Passed;
        result.Job = validationResult.Job;
        result.OptimizationResult =
            validationResult.OptimizationResult;
        result.FrequencyResult =
            validationResult.FrequencyResult;
        result.Report = validationResult.Report;
        result.Wait = waitResult;

        if (!result.Passed)
        {
            result.Error = string.IsNullOrWhiteSpace(
                validationResult.Report.Summary)
                ? "联合优化频率验证未通过。"
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
                    .CalculationOptimizationFrequencyValidation)
                != null;

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
                "联合优化频率 Skill 缺少原子 Skill：" + skillId);
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

    private async Task<TaskResult> InvokeTwoInputsAsync(
        CalculationInputGenerationRequest request,
        string stepName,
        string skillId,
        string firstName,
        string firstValue,
        string secondName,
        string secondValue,
        CancellationToken cancellationToken)
    {
        ISkill? skill = _skills.Resolve(skillId);

        if (skill == null)
        {
            throw new InvalidOperationException(
                "联合优化频率 Skill 缺少原子 Skill：" + skillId);
        }

        TaskRequest stepRequest = new TaskRequest();
        stepRequest.WorkflowId =
            string.IsNullOrWhiteSpace(request.Job.WorkflowId)
                ? request.Job.JobId
                : request.Job.WorkflowId;
        stepRequest.NodeId = stepName;
        stepRequest.SkillId = skillId;
        stepRequest.Inputs[firstName] = firstValue;
        stepRequest.Inputs[secondName] = secondValue;
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
        OptimizationFrequencyCalculationSkillResult result,
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
