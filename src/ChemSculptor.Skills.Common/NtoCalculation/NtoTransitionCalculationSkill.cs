using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.NtoCalculation;

/// <summary>
/// 为源 TDDFT 作业中的一个激发态计算 NTO。
/// 每个 Skill 实例只提交一个派生 NTO 作业。
/// </summary>
public sealed class NtoTransitionCalculationSkill
    : JsonSkill<NtoTransitionRequest, NtoTransitionResult>
{
    private const int PollingIntervalMilliseconds = 500;

    private readonly ICalculationWorkspace _workspace;
    private readonly IComputeBackend _computeBackend;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建单激发态 NTO Skill。</summary>
    public NtoTransitionCalculationSkill(
        ICalculationWorkspace workspace,
        IComputeBackend computeBackend,
        IQuantumProgramAdapterRegistry adapterRegistry,
        ICalculationRepository repository)
    {
        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (computeBackend == null)
        {
            throw new ArgumentNullException(
                nameof(computeBackend));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(
                nameof(adapterRegistry));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _workspace = workspace;
        _computeBackend = computeBackend;
        _adapterRegistry = adapterRegistry;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationNtoTransition);
        _capabilities.Add("calculation.nto");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get
        {
            return CalculationSkillIds.CalculationNtoTransition;
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

    /// <summary>执行一个激发态的 NTO 作业。</summary>
    protected override async Task<NtoTransitionResult>
        ExecuteAsync(
            NtoTransitionRequest request,
            CancellationToken cancellationToken)
    {
        NtoTransitionResult result =
            new NtoTransitionResult();
        result.SourceJobId =
            request.SourceJob.JobId;
        result.StateIndex =
            request.SelectedState.StateIndex;
        result.StateLabel =
            request.SelectedState.StateLabel;

        if (string.IsNullOrWhiteSpace(
                request.SourceInputPath)
            || !File.Exists(request.SourceInputPath))
        {
            result.Error = "源 TDDFT 输入文件不存在。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(
                request.SourceCheckpointPath)
            || !File.Exists(request.SourceCheckpointPath))
        {
            result.Error = "源 TDDFT 检查点文件不存在。";
            return result;
        }

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(request.SourceSpec);

        if (adapter == null
            || !(adapter is INtoProgram ntoProgram))
        {
            result.Error = "当前计算程序不支持 NTO。";
            return result;
        }

        string jobId =
            request.SourceJob.JobId +
            "-nto-" +
            request.SelectedState.StateIndex
                .ToString(
                    System.Globalization.CultureInfo.InvariantCulture) +
            "-" +
            Guid.NewGuid().ToString("N").Substring(0, 8);
        await _workspace.EnsureJobWorkspaceAsync(
            jobId,
            cancellationToken);

        string inputFileName =
            adapter.GetInputFileName(jobId);
        string inputPath = Path.Combine(
            _workspace.GetInputDirectory(jobId),
            inputFileName);
        string runInputPath = Path.Combine(
            _workspace.GetRunDirectory(jobId),
            inputFileName);
        string outputPath =
            _workspace.GetJobOutputPath(jobId);
        string checkpointFileName = jobId + ".chk";
        string runCheckpointPath = Path.Combine(
            _workspace.GetRunDirectory(jobId),
            checkpointFileName);

        NtoInputWriteRequest writeRequest =
            new NtoInputWriteRequest();
        writeRequest.SourceInputPath =
            request.SourceInputPath;
        writeRequest.TargetInputPath = inputPath;
        writeRequest.CheckpointFileName =
            checkpointFileName;
        writeRequest.TransitionIndex =
            request.SelectedState.StateIndex;
        await ntoProgram.WriteNtoInputAsync(
            writeRequest,
            cancellationToken);
        File.Copy(inputPath, runInputPath, true);
        File.Copy(
            request.SourceCheckpointPath,
            runCheckpointPath,
            true);

        CalculationSpec ntoSpec =
            CloneForNto(request.SourceSpec);
        CalculationJob job = new CalculationJob();
        job.JobId = jobId;
        job.TaskId = request.SourceJob.TaskId;
        job.SessionId = request.SourceJob.SessionId;
        job.WorkflowId = request.SourceJob.WorkflowId;
        job.ParentJobId = request.SourceJob.JobId;
        job.RootWorkflowId =
            request.SourceJob.RootWorkflowId;
        job.GeometryId = request.SourceJob.GeometryId;
        job.Goal =
            "NTO Transition " +
            request.SelectedState.StateIndex.ToString();
        job.Spec = ntoSpec;
        job.State = CalculationJobState.InputGenerated;
        job.WorkspaceDirectory =
            _workspace.GetJobDirectory(jobId);
        job.RunDirectory =
            _workspace.GetRunDirectory(jobId);
        job.SourceInputFilePath = inputPath;
        job.InputFilePath = runInputPath;
        job.OutputFilePath = outputPath;
        await _repository.SaveJobAsync(
            job,
            cancellationToken);

        CalculationExecutionContext context =
            adapter.BuildExecutionContext(
                job,
                ntoSpec);
        job.State = CalculationJobState.Running;
        job.StartedAt = DateTimeOffset.UtcNow;
        await _repository.SaveJobAsync(
            job,
            cancellationToken);
        await _computeBackend.SubmitAsync(
            job,
            context,
            cancellationToken);

        CalculationJobState state = CalculationJobState.Running;

        while (!IsTerminalState(state))
        {
            state = await _computeBackend.GetStatusAsync(
                job,
                cancellationToken);

            if (!IsTerminalState(state))
            {
                await Task.Delay(
                    PollingIntervalMilliseconds,
                    cancellationToken);
            }
        }

        job.State = state;
        job.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveJobAsync(
            job,
            cancellationToken);

        if (state != CalculationJobState.Completed)
        {
            result.JobId = jobId;
            result.Error =
                "NTO 作业没有正常完成，最终状态：" +
                state.ToString();
            return result;
        }

        await adapter.PostProcessAsync(
            job,
            cancellationToken);
        NtoTransitionResult parsed =
            await ntoProgram.ParseNtoOutputAsync(
                outputPath,
                request.SelectedState,
                cancellationToken);
        parsed.JobId = jobId;
        parsed.SourceJobId = request.SourceJob.JobId;
        parsed.StateIndex =
            request.SelectedState.StateIndex;
        parsed.StateLabel =
            request.SelectedState.StateLabel;
        parsed.OutputFilePath = outputPath;
        parsed.Artifacts = CollectArtifacts(
            adapter,
            job,
            parsed);
        return parsed;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static CalculationSpec CloneForNto(
        CalculationSpec source)
    {
        CalculationSpec clone = new CalculationSpec();
        clone.TaskType = CalculationTaskType.Nto;
        clone.Program = source.Program;
        clone.Method = source.Method;
        clone.Basis = source.Basis;
        clone.Charge = source.Charge;
        clone.Multiplicity = source.Multiplicity;
        clone.ElectronicStateObjective =
            source.ElectronicStateObjective;
        clone.TargetMultiplicity =
            source.TargetMultiplicity;
        clone.TargetStateLabel =
            source.TargetStateLabel;
        clone.Solvent = source.Solvent;
        clone.ExtraOptions =
            new Dictionary<string, string>(
                source.ExtraOptions,
                StringComparer.OrdinalIgnoreCase);
        clone.Parameters =
            new List<CalculationParameter>();
        return clone;
    }

    private static List<CalculationArtifactDescriptor>
        CollectArtifacts(
            IQuantumProgramAdapter adapter,
            CalculationJob job,
            NtoTransitionResult result)
    {
        CalculationResult artifactResult =
            new CalculationResult();
        artifactResult.JobId = job.JobId;
        artifactResult.Program = job.Spec.Program;
        artifactResult.Method = job.Spec.Method;
        artifactResult.Basis = job.Spec.Basis;
        artifactResult.NormalTermination =
            result.NormalTermination;
        artifactResult.OutputFilePath =
            result.OutputFilePath;
        CalculationArtifactDiscoveryContext context =
            new CalculationArtifactDiscoveryContext();
        context.Job = job;
        context.Result = artifactResult;
        return new List<CalculationArtifactDescriptor>(
            CalculationArtifactCollector.Collect(
                job.RunDirectory,
                adapter.GetArtifactPatterns(context)));
    }

    private static bool IsTerminalState(
        CalculationJobState state)
    {
        return state == CalculationJobState.Completed
            || state == CalculationJobState.Failed
            || state == CalculationJobState.Canceled;
    }
}
