using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.Anomaly.Recovery;

namespace ChemSculptor.Skills.Gaussian.Anomaly.Recovery;

/// <summary>Gaussian 派生恢复作业创建器。</summary>
public sealed class GaussianRecoveryJobProvider
    : IRecoveryJobProvider
{
    private readonly ICalculationWorkspace _workspace;
    private readonly ICalculationRepository _repository;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly GaussianRecoveryInputWriter _inputWriter;

    /// <summary>创建 Gaussian 派生恢复作业提供器。</summary>
    public GaussianRecoveryJobProvider(
        ICalculationWorkspace workspace,
        ICalculationRepository repository,
        IQuantumProgramAdapterRegistry adapterRegistry,
        GaussianRecoveryInputWriter inputWriter)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        if (inputWriter == null)
        {
            throw new ArgumentNullException(nameof(inputWriter));
        }

        _workspace = workspace;
        _repository = repository;
        _adapterRegistry = adapterRegistry;
        _inputWriter = inputWriter;
    }

    /// <summary>适用的计算程序。</summary>
    public string Program
    {
        get { return Gaussian16ProgramAdapter.ProgramNameValue; }
    }

    /// <summary>判断是否能够创建派生作业。</summary>
    public bool CanPrepare(
        RecoveryJobPreparationRequest request)
    {
        if (request == null
            || request.SourceJob == null
            || request.Plan == null
            || request.Plan.Option == null)
        {
            return false;
        }

        if (!string.Equals(
            request.SourceJob.Spec.Program,
            Program,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        CorrectionOption option = request.Plan.Option;

        if (request.SourceJob.Spec.TaskType
            == CalculationTaskType.Optimization)
        {
            return option.CanUseForRestart
                && (IsOptimizationCorrectionIntent(
                        option.IntentCode)
                    || string.Equals(
                        option.IntentCode,
                        CorrectionIntentCodes.WavefunctionOptimization,
                        StringComparison.OrdinalIgnoreCase)
                    || string.Equals(
                        option.IntentCode,
                        CorrectionIntentCodes.ChangeSpinMultiplicity,
                        StringComparison.OrdinalIgnoreCase));
        }

        if (!option.CanUseForRestart
            || !string.Equals(
                option.IntentCode,
                CorrectionIntentCodes.ChangeSpinMultiplicity,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return TryGetTargetMultiplicity(
            option,
            out int targetMultiplicity);
    }

    /// <summary>创建派生 Gaussian 作业，但不提交运行。</summary>
    public async Task<RecoveryJobPreparationResult> PrepareAsync(
        RecoveryJobPreparationRequest request,
        CancellationToken cancellationToken = default)
    {
        RecoveryJobPreparationResult result =
            new RecoveryJobPreparationResult();

        if (!CanPrepare(request))
        {
            result.Succeeded = false;
            result.Error = "当前修正计划不能创建派生 Gaussian 作业。";
            return result;
        }

        if (request.SourceJob.Spec.TaskType
            == CalculationTaskType.Optimization)
        {
            return await PrepareOptimizationAsync(
                request,
                cancellationToken);
        }

        int targetMultiplicity;
        TryGetTargetMultiplicity(
            request.Plan.Option!,
            out targetMultiplicity);

        CalculationJob sourceJob = request.SourceJob;
        string sourceInputPath = ResolveSourceInputPath(sourceJob);

        if (sourceInputPath.Length == 0)
        {
            result.Succeeded = false;
            result.Error = "原始 Gaussian 输入文件不存在。";
            return result;
        }

        string recoveryJobId = "job-" + Guid.NewGuid().ToString("N");
        await _workspace.EnsureJobWorkspaceAsync(
            recoveryJobId,
            cancellationToken);

        CalculationSpec recoverySpec = CloneSpec(sourceJob.Spec);
        recoverySpec.ElectronicStateObjective =
            ElectronicStateObjective.TargetSpinState;
        recoverySpec.TargetMultiplicity = targetMultiplicity;
        recoverySpec.TargetStateLabel = string.Empty;
        recoverySpec.Multiplicity = targetMultiplicity;
        UpdateMultiplicityParameter(
            recoverySpec,
            targetMultiplicity);

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(recoverySpec);

        if (adapter == null)
        {
            result.Succeeded = false;
            result.Error = "没有可处理当前计算方案的适配器。";
            return result;
        }

        string inputFileName = adapter.GetInputFileName(recoveryJobId);
        string inputPath = Path.Combine(
            _workspace.GetInputDirectory(recoveryJobId),
            inputFileName);
        string runInputPath = Path.Combine(
            _workspace.GetRunDirectory(recoveryJobId),
            inputFileName);
        string outputPath =
            _workspace.GetJobOutputPath(recoveryJobId);

        GaussianRecoveryInputWriteResult writeResult =
            await _inputWriter.ChangeMultiplicityAsync(
                sourceInputPath,
                inputPath,
                targetMultiplicity,
                cancellationToken);

        if (!writeResult.Succeeded)
        {
            result.Succeeded = false;
            result.Error = writeResult.Error;
            return result;
        }

        File.Copy(inputPath, runInputPath, true);

        string rootWorkflowId = ResolveRootWorkflowId(sourceJob);
        int attemptNumber = sourceJob.AttemptNumber + 1;

        CalculationJob recoveryJob = new CalculationJob();
        recoveryJob.JobId = recoveryJobId;
        recoveryJob.TaskId = sourceJob.TaskId;
        recoveryJob.SessionId = sourceJob.SessionId;
        recoveryJob.WorkflowId = sourceJob.WorkflowId;
        recoveryJob.ParentJobId = sourceJob.JobId;
        recoveryJob.RootWorkflowId = rootWorkflowId;
        recoveryJob.AttemptNumber = attemptNumber;
        recoveryJob.CorrectionPlanId = request.Plan.Id;
        recoveryJob.GeometryId = sourceJob.GeometryId;
        recoveryJob.Goal = sourceJob.Goal;
        recoveryJob.Spec = recoverySpec;
        recoveryJob.State = CalculationJobState.InputGenerated;
        recoveryJob.WorkspaceDirectory =
            _workspace.GetJobDirectory(recoveryJobId);
        recoveryJob.RunDirectory =
            _workspace.GetRunDirectory(recoveryJobId);
        recoveryJob.SourceInputFilePath = inputPath;
        recoveryJob.InputFilePath = runInputPath;
        recoveryJob.OutputFilePath = outputPath;

        await _repository.SaveJobAsync(
            recoveryJob,
            cancellationToken);

        RecoveryAttempt attempt = new RecoveryAttempt();
        attempt.Id = "attempt-" + Guid.NewGuid().ToString("N");
        attempt.RootWorkflowId = rootWorkflowId;
        attempt.ParentJobId = sourceJob.JobId;
        attempt.RecoveryJobId = recoveryJobId;
        attempt.AttemptNumber = attemptNumber;
        attempt.CorrectionPlanId = request.Plan.Id;

        result.Succeeded = true;
        result.RecoveryJob = recoveryJob;
        result.Attempt = attempt;
        return result;
    }

    private async Task<RecoveryJobPreparationResult>
        PrepareOptimizationAsync(
            RecoveryJobPreparationRequest request,
            CancellationToken cancellationToken)
    {
        RecoveryJobPreparationResult result =
            new RecoveryJobPreparationResult();
        CalculationJob sourceJob = request.SourceJob;
        string sourceInputPath = ResolveSourceInputPath(sourceJob);

        if (sourceInputPath.Length == 0)
        {
            result.Succeeded = false;
            result.Error = "原始 Gaussian 输入文件不存在。";
            return result;
        }

        string recoveryJobId = "job-" + Guid.NewGuid().ToString("N");
        await _workspace.EnsureJobWorkspaceAsync(
            recoveryJobId,
            cancellationToken);

        CalculationSpec recoverySpec = CloneSpec(sourceJob.Spec);
        ApplyOptimizationChanges(
            recoverySpec,
            request.Plan.Option!);

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(recoverySpec);

        if (adapter == null)
        {
            result.Succeeded = false;
            result.Error = "没有可处理当前优化方案的适配器。";
            return result;
        }

        string inputFileName = adapter.GetInputFileName(recoveryJobId);
        string inputPath = Path.Combine(
            _workspace.GetInputDirectory(recoveryJobId),
            inputFileName);
        string runInputPath = Path.Combine(
            _workspace.GetRunDirectory(recoveryJobId),
            inputFileName);
        string outputPath =
            _workspace.GetJobOutputPath(recoveryJobId);

        GaussianRecoveryInputWriteResult writeResult =
            await _inputWriter.ApplyOptimizationCorrectionsAsync(
                sourceInputPath,
                inputPath,
                recoverySpec,
                string.Equals(
                    request.Plan.Option!.IntentCode,
                    CorrectionIntentCodes.WavefunctionOptimization,
                    StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (!writeResult.Succeeded)
        {
            result.Succeeded = false;
            result.Error = writeResult.Error;
            return result;
        }

        File.Copy(inputPath, runInputPath, true);

        string rootWorkflowId = ResolveRootWorkflowId(sourceJob);
        int attemptNumber = sourceJob.AttemptNumber + 1;

        CalculationJob recoveryJob = new CalculationJob();
        recoveryJob.JobId = recoveryJobId;
        recoveryJob.TaskId = sourceJob.TaskId;
        recoveryJob.SessionId = sourceJob.SessionId;
        recoveryJob.WorkflowId = sourceJob.WorkflowId;
        recoveryJob.ParentJobId = sourceJob.JobId;
        recoveryJob.RootWorkflowId = rootWorkflowId;
        recoveryJob.AttemptNumber = attemptNumber;
        recoveryJob.CorrectionPlanId = request.Plan.Id;
        recoveryJob.GeometryId = sourceJob.GeometryId;
        recoveryJob.Goal = sourceJob.Goal;
        recoveryJob.Spec = recoverySpec;
        recoveryJob.State = CalculationJobState.InputGenerated;
        recoveryJob.WorkspaceDirectory =
            _workspace.GetJobDirectory(recoveryJobId);
        recoveryJob.RunDirectory =
            _workspace.GetRunDirectory(recoveryJobId);
        recoveryJob.SourceInputFilePath = inputPath;
        recoveryJob.InputFilePath = runInputPath;
        recoveryJob.OutputFilePath = outputPath;

        await _repository.SaveJobAsync(
            recoveryJob,
            cancellationToken);

        RecoveryAttempt attempt = new RecoveryAttempt();
        attempt.Id = "attempt-" + Guid.NewGuid().ToString("N");
        attempt.RootWorkflowId = rootWorkflowId;
        attempt.ParentJobId = sourceJob.JobId;
        attempt.RecoveryJobId = recoveryJobId;
        attempt.AttemptNumber = attemptNumber;
        attempt.CorrectionPlanId = request.Plan.Id;

        result.Succeeded = true;
        result.RecoveryJob = recoveryJob;
        result.Attempt = attempt;
        return result;
    }

    private static bool IsOptimizationCorrectionIntent(
        string intentCode)
    {
        return string.Equals(
                intentCode,
                CorrectionIntentCodes.IncreaseOptimizationMaxCycles,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                intentCode,
                CorrectionIntentCodes.RestartOptimization,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                intentCode,
                CorrectionIntentCodes.UseOptimizationCalcFc,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                intentCode,
                CorrectionIntentCodes.UseOptimizationReadFc,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                intentCode,
                CorrectionIntentCodes.ReuseScfRecovery,
                StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyOptimizationChanges(
        CalculationSpec spec,
        CorrectionOption option)
    {
        for (int index = 0;
            index < option.Changes.Count;
            index++)
        {
            CorrectionChange change = option.Changes[index];

            if (string.Equals(
                change.Name,
                CalculationOptionKeys.OptimizationMaxCycles,
                StringComparison.OrdinalIgnoreCase))
            {
                int maxCycles;

                if (int.TryParse(
                        change.NewValue,
                        out maxCycles)
                    && maxCycles > 0)
                {
                    SetExtraOption(
                        spec,
                        change.Name,
                        maxCycles.ToString(
                            System.Globalization.CultureInfo
                                .InvariantCulture));
                }
            }
            else if (string.Equals(
                change.Name,
                CalculationOptionKeys.OptimizationRestart,
                StringComparison.OrdinalIgnoreCase))
            {
                SetExtraOption(
                    spec,
                    change.Name,
                    change.NewValue);
            }
            else if (string.Equals(
                change.Name,
                CalculationOptionKeys.OptimizationInitialHessian,
                StringComparison.OrdinalIgnoreCase))
            {
                SetExtraOption(
                    spec,
                    change.Name,
                    change.NewValue);
            }
            else if (string.Equals(
                change.Name,
                CalculationOptionKeys.ScfIterationLimit,
                StringComparison.OrdinalIgnoreCase))
            {
                SetExtraOption(
                    spec,
                    change.Name,
                    change.NewValue);
            }
            else if (string.Equals(
                change.Name,
                "multiplicity",
                StringComparison.OrdinalIgnoreCase))
            {
                int multiplicity;

                if (int.TryParse(
                        change.NewValue,
                        out multiplicity)
                    && multiplicity > 0)
                {
                    spec.Multiplicity = multiplicity;
                    spec.ElectronicStateObjective =
                        ElectronicStateObjective.TargetSpinState;
                    spec.TargetMultiplicity = multiplicity;
                    UpdateMultiplicityParameter(
                        spec,
                        multiplicity);
                }
            }
        }
    }

    private static void SetExtraOption(
        CalculationSpec spec,
        string key,
        string value)
    {
        spec.ExtraOptions[key] = value;

        for (int index = 0;
            index < spec.Parameters.Count;
            index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                parameter.CurrentValue = value;
                parameter.Source = ParameterSource.Agent;
                return;
            }
        }
    }

    private static bool TryGetTargetMultiplicity(
        CorrectionOption option,
        out int targetMultiplicity)
    {
        targetMultiplicity = 0;

        for (int index = 0; index < option.Changes.Count; index++)
        {
            CorrectionChange change = option.Changes[index];

            if (string.Equals(
                change.Name,
                "multiplicity",
                StringComparison.OrdinalIgnoreCase)
                && int.TryParse(
                    change.NewValue,
                    out targetMultiplicity)
                && targetMultiplicity > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static string ResolveSourceInputPath(
        CalculationJob sourceJob)
    {
        if (File.Exists(sourceJob.InputFilePath))
        {
            return sourceJob.InputFilePath;
        }

        if (File.Exists(sourceJob.SourceInputFilePath))
        {
            return sourceJob.SourceInputFilePath;
        }

        return string.Empty;
    }

    private static string ResolveRootWorkflowId(
        CalculationJob sourceJob)
    {
        if (!string.IsNullOrWhiteSpace(sourceJob.RootWorkflowId))
        {
            return sourceJob.RootWorkflowId;
        }

        if (!string.IsNullOrWhiteSpace(sourceJob.WorkflowId))
        {
            return sourceJob.WorkflowId;
        }

        return sourceJob.JobId;
    }

    private static CalculationSpec CloneSpec(
        CalculationSpec source)
    {
        CalculationSpec clone = new CalculationSpec();
        clone.TaskType = source.TaskType;
        clone.Program = source.Program;
        clone.Method = source.Method;
        clone.Basis = source.Basis;
        clone.Charge = source.Charge;
        clone.Multiplicity = source.Multiplicity;
        clone.ElectronicStateObjective = source.ElectronicStateObjective;
        clone.TargetMultiplicity = source.TargetMultiplicity;
        clone.TargetStateLabel = source.TargetStateLabel;
        clone.Solvent = source.Solvent;

        clone.ExtraOptions =
            new Dictionary<string, string>(
                source.ExtraOptions,
                StringComparer.OrdinalIgnoreCase);
        clone.Parameters = new List<CalculationParameter>();

        for (int index = 0; index < source.Parameters.Count; index++)
        {
            clone.Parameters.Add(
                CloneParameter(source.Parameters[index]));
        }

        return clone;
    }

    private static CalculationParameter CloneParameter(
        CalculationParameter source)
    {
        CalculationParameter clone = new CalculationParameter();
        clone.Name = source.Name;
        clone.DisplayName = source.DisplayName;
        clone.CurrentValue = source.CurrentValue;
        clone.DefaultValue = source.DefaultValue;
        clone.Source = source.Source;
        clone.RiskLevel = source.RiskLevel;
        clone.RequiresApproval = source.RequiresApproval;
        clone.Description = source.Description;
        return clone;
    }

    private static void UpdateMultiplicityParameter(
        CalculationSpec spec,
        int multiplicity)
    {
        for (int index = 0; index < spec.Parameters.Count; index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                "multiplicity",
                StringComparison.OrdinalIgnoreCase))
            {
                parameter.CurrentValue = multiplicity.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
                parameter.Source = ParameterSource.User;
                parameter.Description =
                    "由波函数稳定性修正方案确定。";
                return;
            }
        }
    }
}
