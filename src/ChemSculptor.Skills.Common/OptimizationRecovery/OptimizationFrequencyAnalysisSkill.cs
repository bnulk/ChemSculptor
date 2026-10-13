using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>对最终优化几何执行一次高斯振动分析。</summary>
public sealed class OptimizationFrequencyAnalysisSkill : ISkill
{
    private const int PollingIntervalMilliseconds = 500;
    private const string OptimizationKey = "optimization";
    private const string RecoveryExecutionKey = "recoveryExecution";
    private const string StabilityRecoveryExecutionKey =
        "stabilityRecoveryExecution";

    private readonly ICalculationWorkspace _workspace;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly IComputeBackend _computeBackend;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化振动分析 Skill。</summary>
    public OptimizationFrequencyAnalysisSkill(
        ICalculationWorkspace workspace,
        IQuantumProgramAdapterRegistry adapterRegistry,
        IComputeBackend computeBackend,
        ICalculationRepository repository)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(
                nameof(adapterRegistry));
        }

        if (computeBackend == null)
        {
            throw new ArgumentNullException(nameof(computeBackend));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _workspace = workspace;
        _adapterRegistry = adapterRegistry;
        _computeBackend = computeBackend;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationFrequencyAnalysis);
        _capabilities.Add("optimization.frequency-analysis");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationFrequencyAnalysis;
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

    /// <summary>执行一次振动分析作业。</summary>
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
                "振动分析节点缺少 optimization 输入。");
        }

        OptimizationCalculationSkillResult optimization =
            SkillJson.Deserialize<
                OptimizationCalculationSkillResult>(
                    optimizationJson);
        CalculationJob finalJob =
            ResolveFinalJob(request, optimization);
        FrequencyAnalysisResult frequencyResult =
            await RunFrequencyAsync(
                finalJob,
                cancellationToken);

        TaskResult result = new TaskResult();
        result.WorkflowId = request.WorkflowId;
        result.NodeId = request.NodeId;
        result.Succeeded = frequencyResult.NormalTermination;
        result.Output = SkillJson.Serialize(frequencyResult);

        if (!frequencyResult.NormalTermination)
        {
            result.Diagnostics =
                "振动分析没有正常结束。";
        }

        return result;
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
        string? stabilityJson;

        if (request.Inputs.TryGetValue(
                StabilityRecoveryExecutionKey,
                out stabilityJson)
            && !string.IsNullOrWhiteSpace(stabilityJson))
        {
            RecoveryJobExecutionResult stabilityRecovery =
                SkillJson.Deserialize<
                    RecoveryJobExecutionResult>(
                        stabilityJson);

            if (stabilityRecovery.RecoveryJob != null)
            {
                return stabilityRecovery.RecoveryJob;
            }
        }

        string? recoveryJson;

        if (request.Inputs.TryGetValue(
                RecoveryExecutionKey,
                out recoveryJson)
            && !string.IsNullOrWhiteSpace(recoveryJson))
        {
            OptimizationRecoveryExecutionResult recovery =
                SkillJson.Deserialize<
                    OptimizationRecoveryExecutionResult>(
                        recoveryJson);

            if (recovery.RecoveryJob != null)
            {
                return recovery.RecoveryJob;
            }
        }

        return optimization.Job;
    }

    private async Task<FrequencyAnalysisResult> RunFrequencyAsync(
        CalculationJob finalJob,
        CancellationToken cancellationToken)
    {
        CalculationSpec frequencySpec =
            CloneForFrequency(finalJob.Spec);
        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(frequencySpec);

        if (adapter == null)
        {
            throw new InvalidOperationException(
                "没有可处理振动分析的适配器。");
        }

        if (!(adapter is IFrequencyAnalysisProgram frequencyProgram))
        {
            throw new NotSupportedException(
                "当前程序适配器不支持振动分析。");
        }

        string sourceInputPath =
            finalJob.InputFilePath;
        string sourceCheckpointPath =
            ResolveCheckpointPath(finalJob);

        if (string.IsNullOrWhiteSpace(sourceCheckpointPath)
            || !File.Exists(sourceCheckpointPath))
        {
            throw new FileNotFoundException(
                "最终优化作业的检查点文件不存在。",
                sourceCheckpointPath);
        }

        string frequencyJobId =
            finalJob.JobId +
            "-freq-" +
            Guid.NewGuid().ToString("N").Substring(0, 8);
        await _workspace.EnsureJobWorkspaceAsync(
            frequencyJobId,
            cancellationToken);

        string inputFileName = frequencyJobId + ".gjf";
        string inputPath = Path.Combine(
            _workspace.GetInputDirectory(frequencyJobId),
            inputFileName);
        string runInputPath = Path.Combine(
            _workspace.GetRunDirectory(frequencyJobId),
            inputFileName);
        string outputPath =
            _workspace.GetJobOutputPath(frequencyJobId);
        string checkpointFileName = frequencyJobId + ".chk";
        string runCheckpointPath = Path.Combine(
            _workspace.GetRunDirectory(frequencyJobId),
            checkpointFileName);

        await frequencyProgram.WriteFrequencyInputAsync(
            frequencySpec,
            sourceInputPath,
            inputPath,
            checkpointFileName,
            cancellationToken);
        File.Copy(inputPath, runInputPath, true);
        File.Copy(
            sourceCheckpointPath,
            runCheckpointPath,
            true);

        CalculationJob frequencyJob = new CalculationJob();
        frequencyJob.JobId = frequencyJobId;
        frequencyJob.TaskId = finalJob.TaskId;
        frequencyJob.SessionId = finalJob.SessionId;
        frequencyJob.WorkflowId = finalJob.WorkflowId;
        frequencyJob.ParentJobId = finalJob.JobId;
        frequencyJob.RootWorkflowId = finalJob.RootWorkflowId;
        frequencyJob.AttemptNumber = finalJob.AttemptNumber;
        frequencyJob.GeometryId = finalJob.GeometryId;
        frequencyJob.Goal = "振动分析";
        frequencyJob.Spec = frequencySpec;
        frequencyJob.State = CalculationJobState.Running;
        frequencyJob.WorkspaceDirectory =
            _workspace.GetJobDirectory(frequencyJobId);
        frequencyJob.RunDirectory =
            _workspace.GetRunDirectory(frequencyJobId);
        frequencyJob.SourceInputFilePath = inputPath;
        frequencyJob.InputFilePath = runInputPath;
        frequencyJob.OutputFilePath = outputPath;
        frequencyJob.StartedAt = DateTimeOffset.UtcNow;

        await _repository.SaveJobAsync(
            frequencyJob,
            cancellationToken);
        CalculationExecutionContext executionContext =
            adapter.BuildExecutionContext(
                frequencyJob,
                frequencySpec);
        await _computeBackend.SubmitAsync(
            frequencyJob,
            executionContext,
            cancellationToken);

        CalculationJobState state = CalculationJobState.Running;

        while (!IsTerminalState(state))
        {
            state = await _computeBackend.GetStatusAsync(
                frequencyJob,
                cancellationToken);

            if (!IsTerminalState(state))
            {
                await Task.Delay(
                    PollingIntervalMilliseconds,
                    cancellationToken);
            }
        }

        frequencyJob.State = state;
        frequencyJob.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveJobAsync(
            frequencyJob,
            cancellationToken);

        FrequencyAnalysisResult result =
            await frequencyProgram.ParseFrequencyOutputAsync(
                outputPath,
                cancellationToken);
        result.JobId = frequencyJobId;
        result.Program = frequencySpec.Program;
        result.Method = frequencySpec.Method;
        result.Basis = frequencySpec.Basis;
        result.OutputFilePath = outputPath;

        CalculationArtifactDiscoveryContext artifactContext =
            new CalculationArtifactDiscoveryContext();
        artifactContext.Job = frequencyJob;
        CalculationResult artifactResult =
            new CalculationResult();
        artifactResult.JobId = frequencyJobId;
        artifactResult.Program = frequencySpec.Program;
        artifactResult.Method = frequencySpec.Method;
        artifactResult.Basis = frequencySpec.Basis;
        artifactResult.OutputFilePath = outputPath;
        artifactResult.NormalTermination =
            result.NormalTermination;
        artifactContext.Result = artifactResult;
        result.Artifacts = CalculationArtifactCollector.Collect(
            frequencyJob.RunDirectory,
            adapter.GetArtifactPatterns(artifactContext));
        return result;
    }

    private static CalculationSpec CloneForFrequency(
        CalculationSpec source)
    {
        CalculationSpec clone = new CalculationSpec();
        clone.TaskType = CalculationTaskType.Frequency;
        clone.Program = source.Program;
        clone.Method = source.Method;
        clone.Basis = source.Basis;
        clone.Charge = source.Charge;
        clone.Multiplicity = source.Multiplicity;
        clone.ElectronicStateObjective =
            source.ElectronicStateObjective;
        clone.TargetMultiplicity = source.TargetMultiplicity;
        clone.TargetStateLabel = source.TargetStateLabel;
        clone.Solvent = source.Solvent;
        clone.ExtraOptions =
            new Dictionary<string, string>(
                source.ExtraOptions,
                StringComparer.OrdinalIgnoreCase);
        clone.Parameters = new List<CalculationParameter>();

        for (int index = 0;
            index < source.Parameters.Count;
            index++)
        {
            CalculationParameter parameter =
                source.Parameters[index];
            CalculationParameter copy =
                new CalculationParameter();
            copy.Name = parameter.Name;
            copy.DisplayName = parameter.DisplayName;
            copy.CurrentValue = parameter.CurrentValue;
            copy.DefaultValue = parameter.DefaultValue;
            copy.Source = parameter.Source;
            copy.RiskLevel = parameter.RiskLevel;
            copy.RequiresApproval =
                parameter.RequiresApproval;
            copy.Description = parameter.Description;
            clone.Parameters.Add(copy);
        }

        return clone;
    }

    private static string ResolveCheckpointPath(
        CalculationJob job)
    {
        string fromInput = Path.ChangeExtension(
            job.InputFilePath,
            ".chk");

        if (File.Exists(fromInput))
        {
            return fromInput;
        }

        if (!string.IsNullOrWhiteSpace(job.RunDirectory)
            && Directory.Exists(job.RunDirectory))
        {
            string[] checkpoints = Directory.GetFiles(
                job.RunDirectory,
                "*.chk",
                SearchOption.TopDirectoryOnly);

            if (checkpoints.Length > 0)
            {
                return checkpoints[0];
            }
        }

        return fromInput;
    }

    private static bool IsTerminalState(
        CalculationJobState state)
    {
        return state == CalculationJobState.Completed
            || state == CalculationJobState.Failed
            || state == CalculationJobState.Canceled;
    }
}
