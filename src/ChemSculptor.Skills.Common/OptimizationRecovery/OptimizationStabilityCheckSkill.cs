using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>对最终优化几何执行波函数稳定性检查。</summary>
public sealed class OptimizationStabilityCheckSkill : ISkill
{
    private const string OptimizationKey = "optimization";
    private const string RecoveryExecutionKey = "recoveryExecution";

    private readonly IAnomalyProviderRegistry _registry;
    private readonly IAnomalyRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化稳定性检查 Skill。</summary>
    public OptimizationStabilityCheckSkill(
        IAnomalyProviderRegistry registry,
        IAnomalyRepository repository)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _registry = registry;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.CheckOptimizationStability);
        _capabilities.Add("optimization.stability-check");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.CheckOptimizationStability;
        }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "1.1.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>执行最终几何的稳定性检查。</summary>
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
                "优化稳定性检查节点缺少 optimization 输入。");
        }

        OptimizationCalculationSkillResult optimization =
            SkillJson.Deserialize<
                OptimizationCalculationSkillResult>(
                    optimizationJson);
        CalculationJob finalJob = ResolveFinalJob(
            request,
            optimization);

        if (ShouldSkip(finalJob))
        {
            AnomalyCheckResult skipped =
                CreateSkippedResult(finalJob);
            TaskResult skippedTask =
                BuildTaskResult(request, skipped);
            return skippedTask;
        }

        AnomalyContext context = new AnomalyContext();
        context.Job = finalJob;
        context.OptimizationResult = optimization.Result;

        IAnomalyCheck? selected = null;
        IReadOnlyList<IAnomalyCheck> checks =
            _registry.ListChecks();

        for (int index = 0; index < checks.Count; index++)
        {
            IAnomalyCheck check = checks[index];

            if (!string.Equals(
                    check.Descriptor.Code,
                    CommonAnomalyCheckCodes.WavefunctionStability,
                    StringComparison.OrdinalIgnoreCase)
                || !check.CanCheck(context))
            {
                continue;
            }

            if (selected == null
                || string.Compare(
                    check.Descriptor.ImplementationId,
                    selected.Descriptor.ImplementationId,
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                selected = check;
            }
        }

        AnomalyCheckResult result;

        if (selected == null)
        {
            result = CreateUnavailableResult(finalJob);
        }
        else
        {
            result = await selected.CheckAsync(
                context,
                cancellationToken);
        }

        if (result.Findings.Count > 0)
        {
            AnomalyRecord record = new AnomalyRecord();
            record.Id = "anomaly-" + Guid.NewGuid().ToString("N");
            record.JobId = finalJob.JobId;
            record.WorkflowId = finalJob.WorkflowId;
            record.Status = AnomalyRecordStatus.Open;
            record.Checks.Add(result);
            await _repository.SaveAsync(
                record,
                cancellationToken);
            result.AnomalyRecordId = record.Id;
        }

        return BuildTaskResult(request, result);
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static CalculationJob ResolveFinalJob(
        TaskRequest request,
        OptimizationCalculationSkillResult optimization)
    {
        string? recoveryJson;

        if (request.Inputs.TryGetValue(
                RecoveryExecutionKey,
                out recoveryJson)
            && !string.IsNullOrWhiteSpace(recoveryJson))
        {
            RecoveryJobExecutionResult recovery =
                SkillJson.Deserialize<
                    RecoveryJobExecutionResult>(
                        recoveryJson);

            if (recovery.RecoveryJob != null)
            {
                return recovery.RecoveryJob;
            }
        }

        return optimization.Job;
    }

    private static bool ShouldSkip(CalculationJob job)
    {
        string? value;

        if (!job.Spec.ExtraOptions.TryGetValue(
            CalculationOptionKeys.WavefunctionStabilityCheck,
            out value))
        {
            return false;
        }

        return string.Equals(
            value,
            "Skip",
            StringComparison.OrdinalIgnoreCase);
    }

    private static AnomalyCheckResult CreateSkippedResult(
        CalculationJob job)
    {
        AnomalyCheckResult result =
            CreateBaseResult();
        result.JobId = job.JobId;
        result.Status = AnomalyCheckStatus.Skipped;
        result.SkippedReason =
            "用户明确要求跳过波函数稳定性检查。";
        result.Summary = result.SkippedReason;
        result.StartedAt = DateTimeOffset.UtcNow;
        result.CompletedAt = DateTimeOffset.UtcNow;
        return result;
    }

    private static AnomalyCheckResult CreateUnavailableResult(
        CalculationJob job)
    {
        AnomalyCheckResult result =
            CreateBaseResult();
        result.JobId = job.JobId;
        result.Status = AnomalyCheckStatus.Skipped;
        result.SkippedReason =
            "没有适用于当前任务和计算程序的波函数稳定性检查实现。";
        result.Summary = result.SkippedReason;
        result.StartedAt = DateTimeOffset.UtcNow;
        result.CompletedAt = DateTimeOffset.UtcNow;
        return result;
    }

    private static AnomalyCheckResult CreateBaseResult()
    {
        AnomalyCheckResult result = new AnomalyCheckResult();
        result.Code =
            CommonAnomalyCheckCodes.WavefunctionStability;
        result.DisplayName = "波函数稳定性检查";
        result.Category = AnomalyCategory.Scientific;
        result.Mechanism =
            AnomalyCheckMechanism.AuxiliaryCalculation;
        result.IsRequired = true;
        return result;
    }

    private static TaskResult BuildTaskResult(
        TaskRequest request,
        AnomalyCheckResult result)
    {
        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return taskResult;
    }
}
