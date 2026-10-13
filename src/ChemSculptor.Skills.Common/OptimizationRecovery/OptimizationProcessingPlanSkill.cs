using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>根据优化正常结果或恢复结果生成处理方案。</summary>
public sealed class OptimizationProcessingPlanSkill : ISkill
{
    private const string OptimizationKey = "optimization";
    private const string RecoveryExecutionKey = "recoveryExecution";
    private const string RecoveryValidationKey = "recoveryValidation";

    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化处理方案 Skill。</summary>
    public OptimizationProcessingPlanSkill(
        ICalculationRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationOptimizationProcessingPlan);
        _capabilities.Add("calculation.processing-plan");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationOptimizationProcessingPlan;
        }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>生成并保存优化处理方案。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? optimizationJson;

        if (!request.Inputs.TryGetValue(
            OptimizationKey,
            out optimizationJson)
            || string.IsNullOrWhiteSpace(optimizationJson))
        {
            throw new InvalidOperationException(
                "优化处理方案节点缺少 optimization 输入。");
        }

        OptimizationCalculationSkillResult optimization =
            SkillJson.Deserialize<
                OptimizationCalculationSkillResult>(
                    optimizationJson);
        OptimizationRecoveryExecutionResult? recoveryExecution =
            DeserializeOptional<
                OptimizationRecoveryExecutionResult>(
                    request,
                    RecoveryExecutionKey);
        RecoveryValidationResult? recoveryValidation =
            DeserializeOptional<RecoveryValidationResult>(
                request,
                RecoveryValidationKey);

        CalculationProcessingPlan plan =
            CreatePlan(
                optimization,
                recoveryExecution,
                recoveryValidation);
        await _repository.SaveProcessingPlanAsync(
            plan,
            cancellationToken);

        CalculationWorkflowProcessingPlanSkillResult result =
            new CalculationWorkflowProcessingPlanSkillResult();
        result.Plan = plan;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static CalculationProcessingPlan CreatePlan(
        OptimizationCalculationSkillResult optimization,
        OptimizationRecoveryExecutionResult? recoveryExecution,
        RecoveryValidationResult? recoveryValidation)
    {
        CalculationProcessingPlan plan =
            new CalculationProcessingPlan();
        OptimizationResult finalResult = optimization.Result;
        CalculationJob finalJob = optimization.Job;

        if (recoveryExecution != null
            && recoveryExecution.Result != null
            && recoveryExecution.RecoveryJob != null)
        {
            finalResult = recoveryExecution.Result;
            finalJob = recoveryExecution.RecoveryJob;
        }

        plan.JobId = finalJob.JobId;

        if (finalResult.NormalTermination
            && finalResult.OptimizationConverged == true
            && finalResult.FinalEnergy.HasValue)
        {
            plan.Outcome = CalculationProcessingOutcome.Completed;
            plan.Summary =
                "几何优化正常结束并达到收敛标准。";

            if (recoveryValidation != null
                && recoveryValidation.ResolvedOriginalAnomaly)
            {
                plan.Summary =
                    "几何优化通过恢复作业达到收敛标准。";
            }

            return plan;
        }

        plan.Outcome =
            CalculationProcessingOutcome.NeedsUserInput;
        plan.Summary =
            "几何优化没有达到完成条件，需要人工检查。";

        CalculationProcessingAction action =
            new CalculationProcessingAction();
        action.Code = "optimization.review_output";
        action.ActionType =
            CalculationProcessingActionType.ReviewOutput;
        action.Title = "检查优化输出";
        action.Description =
            "检查优化轨迹、收敛指标和恢复记录。";
        action.RequiresApproval = true;
        plan.Actions.Add(action);
        return plan;
    }

    private static T? DeserializeOptional<T>(
        TaskRequest request,
        string key)
        where T : class
    {
        string? json;

        if (!request.Inputs.TryGetValue(key, out json)
            || string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return SkillJson.Deserialize<T>(json);
    }
}
