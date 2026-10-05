using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.AnomalyWorkflow;

/// <summary>根据修正计划创建派生恢复作业。</summary>
public sealed class RecoveryJobCreationSkill : ISkill
{
    private const string CorrectionPlanKey = "correctionPlan";

    private readonly IAnomalyProviderRegistry _anomalyRegistry;
    private readonly ICalculationRepository _calculationRepository;
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly List<string> _capabilities;

    /// <summary>创建派生作业 Skill。</summary>
    public RecoveryJobCreationSkill(
        IAnomalyProviderRegistry anomalyRegistry,
        ICalculationRepository calculationRepository,
        IAnomalyRepository anomalyRepository)
    {
        if (anomalyRegistry == null)
        {
            throw new ArgumentNullException(nameof(anomalyRegistry));
        }

        if (calculationRepository == null)
        {
            throw new ArgumentNullException(nameof(calculationRepository));
        }

        if (anomalyRepository == null)
        {
            throw new ArgumentNullException(nameof(anomalyRepository));
        }

        _anomalyRegistry = anomalyRegistry;
        _calculationRepository = calculationRepository;
        _anomalyRepository = anomalyRepository;
        _capabilities = new List<string>();
        _capabilities.Add("anomaly.recovery");
        _capabilities.Add("anomaly.recovery-job");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get { return AnomalySkillIds.CreateRecoveryJob; }
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

    /// <summary>创建派生作业并保存恢复尝试。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? planJson;

        if (!request.Inputs.TryGetValue(
            CorrectionPlanKey,
            out planJson)
            || string.IsNullOrWhiteSpace(planJson))
        {
            throw new InvalidOperationException(
                "派生作业节点缺少 correctionPlan 输入。");
        }

        WavefunctionStabilityCorrectionPlanningResult planning =
            SkillJson.Deserialize<
                WavefunctionStabilityCorrectionPlanningResult>(
                planJson);
        RecoveryJobPreparationResult preparation =
            new RecoveryJobPreparationResult();

        if (!planning.PlanCreated || planning.Plan == null)
        {
            preparation.Succeeded = false;
            preparation.Error = "没有可执行的修正计划。";
        }
        else
        {
            preparation = await PrepareAsync(
                planning,
                cancellationToken);
        }

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(preparation);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private async Task<RecoveryJobPreparationResult> PrepareAsync(
        WavefunctionStabilityCorrectionPlanningResult planning,
        CancellationToken cancellationToken)
    {
        CorrectionPlan plan = planning.Plan!;
        CalculationJob? sourceJob =
            await _calculationRepository.GetJobAsync(
                plan.SourceJobId,
                cancellationToken);

        if (sourceJob == null)
        {
            RecoveryJobPreparationResult missingJob =
                new RecoveryJobPreparationResult();
            missingJob.Succeeded = false;
            missingJob.Error = "没有找到原始计算作业。";
            return missingJob;
        }

        IRecoveryJobProvider? provider =
            _anomalyRegistry.ResolveRecoveryJobProvider(
                sourceJob.Spec.Program);

        if (provider == null)
        {
            RecoveryJobPreparationResult missingProvider =
                new RecoveryJobPreparationResult();
            missingProvider.Succeeded = false;
            missingProvider.Error =
                "没有可创建派生作业的程序提供器：" +
                sourceJob.Spec.Program;
            return missingProvider;
        }

        RecoveryJobPreparationRequest preparationRequest =
            new RecoveryJobPreparationRequest();
        preparationRequest.SourceJob = sourceJob;
        preparationRequest.Plan = plan;

        if (!provider.CanPrepare(preparationRequest))
        {
            RecoveryJobPreparationResult unsupported =
                new RecoveryJobPreparationResult();
            unsupported.Succeeded = false;
            unsupported.Error = "当前修正计划尚不能创建派生作业。";
            return unsupported;
        }

        RecoveryJobPreparationResult result =
            await provider.PrepareAsync(
                preparationRequest,
                cancellationToken);

        if (result.Succeeded
            && result.Attempt != null
            && !string.IsNullOrWhiteSpace(planning.AnomalyRecordId))
        {
            AnomalyRecord? record =
                await _anomalyRepository.GetAsync(
                    sourceJob.JobId,
                    planning.AnomalyRecordId,
                    cancellationToken);

            if (record != null)
            {
                record.RecoveryAttempts.Add(result.Attempt);
                record.Status = AnomalyRecordStatus.RecoveryPrepared;
                await _anomalyRepository.SaveAsync(
                    record,
                    cancellationToken);
            }
        }

        return result;
    }
}
