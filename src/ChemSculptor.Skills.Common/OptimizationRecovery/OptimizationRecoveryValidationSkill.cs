using ChemSculptor.Anomaly.Checks;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>验证优化恢复是否解决原始异常。</summary>
public sealed class OptimizationRecoveryValidationSkill : ISkill
{
    private const string OriginalAnomalyKey = "originalAnomaly";
    private const string RecoveryExecutionKey = "recoveryExecution";

    private readonly IAnomalyRepository _anomalyRepository;
    private readonly OptimizationConvergenceCheck _convergenceCheck;
    private readonly List<string> _capabilities;

    /// <summary>创建优化恢复验证 Skill。</summary>
    public OptimizationRecoveryValidationSkill(
        IAnomalyRepository anomalyRepository,
        OptimizationConvergenceCheck convergenceCheck)
    {
        if (anomalyRepository == null)
        {
            throw new ArgumentNullException(
                nameof(anomalyRepository));
        }

        if (convergenceCheck == null)
        {
            throw new ArgumentNullException(
                nameof(convergenceCheck));
        }

        _anomalyRepository = anomalyRepository;
        _convergenceCheck = convergenceCheck;
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.ValidateOptimizationRecovery);
        _capabilities.Add("anomaly.recovery-validation");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.ValidateOptimizationRecovery;
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

    /// <summary>验证优化恢复结果。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? anomalyJson;
        string? executionJson;

        if (!request.Inputs.TryGetValue(
            OriginalAnomalyKey,
            out anomalyJson)
            || string.IsNullOrWhiteSpace(anomalyJson))
        {
            throw new InvalidOperationException(
                "优化恢复验证节点缺少 originalAnomaly 输入。");
        }

        if (!request.Inputs.TryGetValue(
            RecoveryExecutionKey,
            out executionJson)
            || string.IsNullOrWhiteSpace(executionJson))
        {
            throw new InvalidOperationException(
                "优化恢复验证节点缺少 recoveryExecution 输入。");
        }

        AnomalyCheckResult originalAnomaly =
            SkillJson.Deserialize<AnomalyCheckResult>(
                anomalyJson);
        OptimizationRecoveryExecutionResult execution =
            SkillJson.Deserialize<
                OptimizationRecoveryExecutionResult>(
                    executionJson);
        RecoveryValidationResult validation =
            await ValidateAsync(
                originalAnomaly,
                execution,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(validation);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private async Task<RecoveryValidationResult> ValidateAsync(
        AnomalyCheckResult originalAnomaly,
        OptimizationRecoveryExecutionResult execution,
        CancellationToken cancellationToken)
    {
        RecoveryValidationResult result =
            new RecoveryValidationResult();

        if (!execution.Succeeded
            || execution.RecoveryJob == null
            || execution.Result == null)
        {
            result.Succeeded = false;
            result.ResolvedOriginalAnomaly = false;
            result.Summary =
                "优化恢复作业没有产生有效优化结果。";
            await UpdateRecordAsync(
                originalAnomaly,
                AnomalyRecordStatus.Failed,
                cancellationToken);
            return result;
        }

        AnomalyContext recoveryContext = new AnomalyContext();
        recoveryContext.Job = execution.RecoveryJob;
        recoveryContext.OptimizationResult = execution.Result;
        AnomalyCheckResult recoveryCheck =
            await _convergenceCheck.CheckAsync(
                recoveryContext,
                cancellationToken);

        result.Succeeded =
            recoveryCheck.Status == AnomalyCheckStatus.Passed;
        result.ResolvedOriginalAnomaly = result.Succeeded;
        result.Summary = result.Succeeded
            ? "优化恢复后未发现新的收敛异常。"
            : "优化恢复后仍存在异常：" + recoveryCheck.Summary;

        for (int index = 0;
            index < recoveryCheck.Findings.Count;
            index++)
        {
            result.Details.Add(
                recoveryCheck.Findings[index].Code);
        }

        await UpdateRecordAsync(
            originalAnomaly,
            result.ResolvedOriginalAnomaly
                ? AnomalyRecordStatus.Resolved
                : AnomalyRecordStatus.Failed,
            cancellationToken);
        return result;
    }

    private async Task UpdateRecordAsync(
        AnomalyCheckResult originalAnomaly,
        AnomalyRecordStatus status,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(originalAnomaly.JobId)
            || string.IsNullOrWhiteSpace(
                originalAnomaly.AnomalyRecordId))
        {
            return;
        }

        AnomalyRecord? record =
            await _anomalyRepository.GetAsync(
                originalAnomaly.JobId,
                originalAnomaly.AnomalyRecordId,
                cancellationToken);

        if (record == null)
        {
            return;
        }

        record.Status = status;
        await _anomalyRepository.SaveAsync(
            record,
            cancellationToken);
    }
}
