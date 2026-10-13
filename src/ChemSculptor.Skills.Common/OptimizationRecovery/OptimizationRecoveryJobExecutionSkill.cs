using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>执行已经创建的优化派生恢复作业。</summary>
public sealed class OptimizationRecoveryJobExecutionSkill : ISkill
{
    private const int PollingIntervalMilliseconds = 500;
    private const string CorrectionPlanKey = "correctionPlan";
    private const string RecoveryJobKey = "recoveryJob";

    private readonly IComputeBackend _computeBackend;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly ICalculationRepository _calculationRepository;
    private readonly IAnomalyRepository _anomalyRepository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化派生作业执行 Skill。</summary>
    public OptimizationRecoveryJobExecutionSkill(
        IComputeBackend computeBackend,
        IQuantumProgramAdapterRegistry adapterRegistry,
        ICalculationRepository calculationRepository,
        IAnomalyRepository anomalyRepository)
    {
        if (computeBackend == null)
        {
            throw new ArgumentNullException(nameof(computeBackend));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        if (calculationRepository == null)
        {
            throw new ArgumentNullException(
                nameof(calculationRepository));
        }

        if (anomalyRepository == null)
        {
            throw new ArgumentNullException(
                nameof(anomalyRepository));
        }

        _computeBackend = computeBackend;
        _adapterRegistry = adapterRegistry;
        _calculationRepository = calculationRepository;
        _anomalyRepository = anomalyRepository;
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.ExecuteOptimizationRecoveryJob);
        _capabilities.Add("anomaly.execute-recovery-job");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.ExecuteOptimizationRecoveryJob;
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

    /// <summary>执行派生优化作业。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? planJson;
        string? recoveryJobJson;

        if (!request.Inputs.TryGetValue(
            CorrectionPlanKey,
            out planJson)
            || string.IsNullOrWhiteSpace(planJson))
        {
            throw new InvalidOperationException(
                "优化派生作业执行节点缺少 correctionPlan 输入。");
        }

        if (!request.Inputs.TryGetValue(
            RecoveryJobKey,
            out recoveryJobJson)
            || string.IsNullOrWhiteSpace(recoveryJobJson))
        {
            throw new InvalidOperationException(
                "优化派生作业执行节点缺少 recoveryJob 输入。");
        }

        OptimizationCorrectionPlanningResult planning =
            SkillJson.Deserialize<
                OptimizationCorrectionPlanningResult>(
                    planJson);
        RecoveryJobPreparationResult preparation =
            SkillJson.Deserialize<RecoveryJobPreparationResult>(
                recoveryJobJson);
        OptimizationRecoveryExecutionResult execution =
            await ExecuteRecoveryAsync(
                planning,
                preparation,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(execution);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private async Task<OptimizationRecoveryExecutionResult>
        ExecuteRecoveryAsync(
            OptimizationCorrectionPlanningResult planning,
            RecoveryJobPreparationResult preparation,
            CancellationToken cancellationToken)
    {
        OptimizationRecoveryExecutionResult result =
            new OptimizationRecoveryExecutionResult();
        CorrectionPlan? plan = planning.Plan;

        if (!planning.PlanCreated
            || plan == null
            || plan.Option == null)
        {
            result.Message = "没有可执行的优化修正计划。";
            return result;
        }

        CorrectionOption option = plan.Option;
        result.CorrectionIntentCode = option.IntentCode;

        if (option.RequiresApproval)
        {
            result.Message =
                "当前优化修正方案需要人工批准，暂不直接执行。";
            return result;
        }

        if (!preparation.Succeeded
            || preparation.RecoveryJob == null)
        {
            result.Message = preparation.Error.Length == 0
                ? "没有可执行的优化派生作业。"
                : preparation.Error;
            return result;
        }

        CalculationJob recoveryJob = preparation.RecoveryJob;
        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(recoveryJob.Spec);

        if (adapter == null)
        {
            result.Message =
                "没有可处理优化派生作业的计算程序适配器。";
            return result;
        }

        if (!(adapter is IOptimizationOutputParser optimizationParser))
        {
            result.Message =
                "当前程序适配器不支持优化恢复结果解析。";
            return result;
        }

        result.Attempted = true;
        result.RecoveryJob = recoveryJob;

        await MarkRecoveringAsync(
            planning,
            cancellationToken);

        recoveryJob.State = CalculationJobState.Running;
        recoveryJob.StartedAt = DateTimeOffset.UtcNow;
        await _calculationRepository.SaveJobAsync(
            recoveryJob,
            cancellationToken);

        CalculationExecutionContext executionContext =
            adapter.BuildExecutionContext(
                recoveryJob,
                recoveryJob.Spec);

        await _computeBackend.SubmitAsync(
            recoveryJob,
            executionContext,
            cancellationToken);

        CalculationJobState state = CalculationJobState.Running;

        while (!IsTerminalState(state))
        {
            state = await _computeBackend.GetStatusAsync(
                recoveryJob,
                cancellationToken);

            if (!IsTerminalState(state))
            {
                await Task.Delay(
                    PollingIntervalMilliseconds,
                    cancellationToken);
            }
        }

        recoveryJob.State = state;
        recoveryJob.CompletedAt = DateTimeOffset.UtcNow;

        if (state != CalculationJobState.Completed)
        {
            await _calculationRepository.SaveJobAsync(
                recoveryJob,
                cancellationToken);
            result.Message =
                "优化派生作业没有正常完成，最终状态：" +
                state.ToString();
            return result;
        }

        try
        {
            await adapter.PostProcessAsync(
                recoveryJob,
                cancellationToken);
        }
        catch (Exception ex)
        {
            CalculationDiagnostic diagnostic =
                new CalculationDiagnostic();
            diagnostic.Severity =
                CalculationDiagnosticSeverity.Warning;
            diagnostic.Code =
                "optimization-recovery.post_processing_failed";
            diagnostic.Message = ex.Message;
            recoveryJob.Diagnostics.Add(diagnostic);
        }

        OptimizationResult optimizationResult =
            await optimizationParser.ParseOptimizationOutputAsync(
                recoveryJob.OutputFilePath,
                cancellationToken);
        optimizationResult.JobId = recoveryJob.JobId;
        optimizationResult.Program = recoveryJob.Spec.Program;
        optimizationResult.Method = recoveryJob.Spec.Method;
        optimizationResult.Basis = recoveryJob.Spec.Basis;
        optimizationResult.Charge = recoveryJob.Spec.Charge;
        optimizationResult.Multiplicity =
            recoveryJob.Spec.Multiplicity;
        optimizationResult.OutputFilePath =
            recoveryJob.OutputFilePath;

        CalculationResult artifactResult =
            new CalculationResult();
        artifactResult.JobId = recoveryJob.JobId;
        artifactResult.Program = recoveryJob.Spec.Program;
        artifactResult.Method = recoveryJob.Spec.Method;
        artifactResult.Basis = recoveryJob.Spec.Basis;
        artifactResult.Charge = recoveryJob.Spec.Charge;
        artifactResult.Multiplicity =
            recoveryJob.Spec.Multiplicity;
        artifactResult.OutputFilePath =
            recoveryJob.OutputFilePath;
        artifactResult.NormalTermination =
            optimizationResult.NormalTermination;
        artifactResult.Energy = optimizationResult.FinalEnergy;

        CalculationArtifactDiscoveryContext artifactContext =
            new CalculationArtifactDiscoveryContext();
        artifactContext.Job = recoveryJob;
        artifactContext.Result = artifactResult;
        optimizationResult.Artifacts =
            CalculationArtifactCollector.Collect(
                recoveryJob.RunDirectory,
                adapter.GetArtifactPatterns(artifactContext));

        await _calculationRepository.SaveJobAsync(
            recoveryJob,
            cancellationToken);

        result.Succeeded =
            optimizationResult.NormalTermination;
        result.Result = optimizationResult;
        result.Message = result.Succeeded
            ? "优化派生恢复作业已完成。"
            : "优化派生作业已结束，但没有正常终止。";
        return result;
    }

    private async Task MarkRecoveringAsync(
        OptimizationCorrectionPlanningResult planning,
        CancellationToken cancellationToken)
    {
        if (planning.Plan == null
            || string.IsNullOrWhiteSpace(
                planning.Plan.SourceJobId)
            || string.IsNullOrWhiteSpace(
                planning.AnomalyRecordId))
        {
            return;
        }

        AnomalyRecord? record =
            await _anomalyRepository.GetAsync(
                planning.Plan.SourceJobId,
                planning.AnomalyRecordId,
                cancellationToken);

        if (record == null)
        {
            return;
        }

        record.Status = AnomalyRecordStatus.Recovering;
        await _anomalyRepository.SaveAsync(
            record,
            cancellationToken);
    }

    private static bool IsTerminalState(
        CalculationJobState state)
    {
        return state == CalculationJobState.Completed
            || state == CalculationJobState.Failed
            || state == CalculationJobState.Canceled;
    }
}
