using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;

/// <summary>
/// Gaussian 波函数稳定性检查。
/// 优先读取已有输出；没有输出时创建并执行辅助稳定性检查作业。
/// </summary>
public sealed class GaussianWavefunctionStabilityCheckSkill
    : JsonSkill<AnomalyCheckRequest, AnomalyCheckResult>,
      IAnomalyCheck
{
    private const int PollingIntervalMilliseconds = 500;

    private static readonly AnomalyCheckDescriptor DescriptorValue =
        CreateDescriptor();

    private readonly GaussianWavefunctionStabilityParser _parser;
    private readonly GaussianWavefunctionStabilityInputWriter _inputWriter;
    private readonly ICalculationWorkspace _workspace;
    private readonly IComputeBackend _computeBackend;
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建稳定性检查。</summary>
    public GaussianWavefunctionStabilityCheckSkill(
        GaussianWavefunctionStabilityParser parser,
        GaussianWavefunctionStabilityInputWriter inputWriter,
        ICalculationWorkspace workspace,
        IComputeBackend computeBackend,
        IQuantumProgramAdapterRegistry adapterRegistry,
        ICalculationRepository repository)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        if (inputWriter == null)
        {
            throw new ArgumentNullException(nameof(inputWriter));
        }

        if (workspace == null)
        {
            throw new ArgumentNullException(nameof(workspace));
        }

        if (computeBackend == null)
        {
            throw new ArgumentNullException(nameof(computeBackend));
        }

        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _parser = parser;
        _inputWriter = inputWriter;
        _workspace = workspace;
        _computeBackend = computeBackend;
        _adapterRegistry = adapterRegistry;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(AnomalySkillIds.CheckWavefunctionStability);
        _capabilities.Add("gaussian.wavefunction-stability");
    }

    /// <summary>技能名称。</summary>
    public override string Name
    {
        get { return GaussianSkillIds.WavefunctionStabilityCheck; }
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

    /// <summary>检查描述。</summary>
    public AnomalyCheckDescriptor Descriptor
    {
        get { return DescriptorValue; }
    }

    /// <summary>判断是否存在可检查的稳定性输出。</summary>
    public bool CanCheck(AnomalyContext context)
    {
        if (context == null)
        {
            return false;
        }

        if (context.Job == null
            || !string.Equals(
                context.Job.Spec.Program,
                Gaussian16ProgramAdapter.ProgramNameValue,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string? outputPath = GetExistingOutputPath(context);
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return true;
        }

        return ResolveOriginalInputPath(context.Job).Length > 0;
    }

    /// <summary>解析稳定性输出并生成统一检查结果。</summary>
    public async Task<AnomalyCheckResult> CheckAsync(
        AnomalyContext context,
        CancellationToken cancellationToken = default)
    {
        AnomalyCheckResult checkResult = CreateBaseResult();
        checkResult.StartedAt = DateTimeOffset.UtcNow;
        checkResult.JobId = context.Job == null
            ? string.Empty
            : context.Job.JobId;

        if (!CanCheck(context))
        {
            checkResult.Status = AnomalyCheckStatus.Skipped;
            checkResult.SkippedReason =
                "没有可用于波函数稳定性检查的输入或输出。";
            checkResult.Summary = checkResult.SkippedReason;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        try
        {
            string? outputPath = GetExistingOutputPath(context);

            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                WavefunctionStabilityResult existingResult =
                    await _parser.ParseAsync(
                        outputPath,
                        cancellationToken);
                existingResult.CurrentMultiplicity =
                    context.Job!.Spec.Multiplicity;
                existingResult.ElectronicStateObjective =
                    context.Job.Spec.ElectronicStateObjective;
                existingResult.TargetMultiplicity =
                    context.Job.Spec.TargetMultiplicity;
                ApplyStabilityResult(checkResult, existingResult);
                checkResult.CompletedAt = DateTimeOffset.UtcNow;
                return checkResult;
            }

            return await RunAuxiliaryCheckAsync(
                context,
                checkResult,
                cancellationToken);
        }
        catch (Exception ex)
        {
            checkResult.Status = AnomalyCheckStatus.ExecutionFailed;
            checkResult.Summary =
                "波函数稳定性检查执行失败：" + ex.Message;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }
    }

    private async Task<AnomalyCheckResult> RunAuxiliaryCheckAsync(
        AnomalyContext context,
        AnomalyCheckResult checkResult,
        CancellationToken cancellationToken)
    {
        CalculationJob originalJob = context.Job!;
        string sourceInputPath =
            ResolveOriginalInputPath(originalJob);
        string sourceCheckpointPath =
            ResolveOriginalCheckpointPath(originalJob);

        if (sourceCheckpointPath.Length == 0)
        {
            checkResult.Status = AnomalyCheckStatus.ExecutionFailed;
            checkResult.Summary = "没有找到原始计算的 Gaussian 检查点文件。";
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        string stabilityJobId =
            originalJob.JobId +
            "-stability-" +
            Guid.NewGuid().ToString("N").Substring(0, 8);

        await _workspace.EnsureJobWorkspaceAsync(
            stabilityJobId,
            cancellationToken);

        string inputFileName = stabilityJobId + ".gjf";
        string inputPath = Path.Combine(
            _workspace.GetInputDirectory(stabilityJobId),
            inputFileName);
        string runInputPath = Path.Combine(
            _workspace.GetRunDirectory(stabilityJobId),
            inputFileName);
        string outputPath =
            _workspace.GetJobOutputPath(stabilityJobId);
        string checkpointFileName = stabilityJobId + ".chk";
        string runCheckpointPath = Path.Combine(
            _workspace.GetRunDirectory(stabilityJobId),
            checkpointFileName);

        GaussianWavefunctionStabilityInputWriteResult writeResult =
            await _inputWriter.WriteAsync(
                sourceInputPath,
                inputPath,
                checkpointFileName,
                cancellationToken);

        if (!writeResult.IsSupported)
        {
            checkResult.Status = AnomalyCheckStatus.Skipped;
            checkResult.Summary = writeResult.Message;
            checkResult.SkippedReason = writeResult.Message;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        if (!writeResult.Succeeded)
        {
            checkResult.Status = AnomalyCheckStatus.ExecutionFailed;
            checkResult.Summary = writeResult.Message;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        File.Copy(inputPath, runInputPath, true);
        File.Copy(
            sourceCheckpointPath,
            runCheckpointPath,
            true);

        CalculationJob stabilityJob = new CalculationJob();
        stabilityJob.JobId = stabilityJobId;
        stabilityJob.TaskId = originalJob.TaskId;
        stabilityJob.SessionId = originalJob.SessionId;
        stabilityJob.WorkflowId = originalJob.WorkflowId;
        stabilityJob.GeometryId = originalJob.GeometryId;
        stabilityJob.Goal = "波函数稳定性检查";
        stabilityJob.Spec = originalJob.Spec;
        stabilityJob.State = CalculationJobState.Running;
        stabilityJob.WorkspaceDirectory =
            _workspace.GetJobDirectory(stabilityJobId);
        stabilityJob.RunDirectory =
            _workspace.GetRunDirectory(stabilityJobId);
        stabilityJob.SourceInputFilePath = inputPath;
        stabilityJob.InputFilePath = runInputPath;
        stabilityJob.OutputFilePath = outputPath;
        stabilityJob.StartedAt = DateTimeOffset.UtcNow;

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(stabilityJob.Spec);

        if (adapter == null)
        {
            checkResult.Status = AnomalyCheckStatus.ExecutionFailed;
            checkResult.Summary = "没有可处理当前计算方案的适配器。";
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        CalculationExecutionContext executionContext =
            adapter.BuildExecutionContext(
                stabilityJob,
                stabilityJob.Spec);

        await _repository.SaveJobAsync(
            stabilityJob,
            cancellationToken);
        await _computeBackend.SubmitAsync(
            stabilityJob,
            executionContext,
            cancellationToken);

        CalculationJobState state = CalculationJobState.Running;

        while (!IsTerminalState(state))
        {
            state = await _computeBackend.GetStatusAsync(
                stabilityJob,
                cancellationToken);

            if (!IsTerminalState(state))
            {
                await Task.Delay(
                    PollingIntervalMilliseconds,
                    cancellationToken);
            }
        }

        stabilityJob.State = state;
        stabilityJob.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveJobAsync(
            stabilityJob,
            cancellationToken);

        checkResult.AuxiliaryJobId = stabilityJobId;

        if (state != CalculationJobState.Completed)
        {
            checkResult.Status = AnomalyCheckStatus.ExecutionFailed;
            checkResult.Summary =
                "稳定性检查作业没有正常完成，最终状态：" +
                state.ToString();
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        WavefunctionStabilityResult stabilityResult =
            await _parser.ParseAsync(
                outputPath,
                cancellationToken);
        stabilityResult.CurrentMultiplicity =
            originalJob.Spec.Multiplicity;
        stabilityResult.ElectronicStateObjective =
            originalJob.Spec.ElectronicStateObjective;
        stabilityResult.TargetMultiplicity =
            originalJob.Spec.TargetMultiplicity;
        ApplyStabilityResult(checkResult, stabilityResult);
        checkResult.CompletedAt = DateTimeOffset.UtcNow;
        return checkResult;
    }

    private static void ApplyStabilityResult(
        AnomalyCheckResult checkResult,
        WavefunctionStabilityResult stabilityResult)
    {
        checkResult.Evidence = new List<AnomalyEvidence>(
            stabilityResult.Evidence);
        checkResult.WavefunctionStability = stabilityResult;
        checkResult.Summary = stabilityResult.Summary;

        if (stabilityResult.Status == WavefunctionStabilityStatus.Stable)
        {
            checkResult.Status = AnomalyCheckStatus.Passed;
            return;
        }

        if (stabilityResult.Status == WavefunctionStabilityStatus.NotPerformed)
        {
            checkResult.Status = AnomalyCheckStatus.Skipped;
            checkResult.SkippedReason = stabilityResult.Summary;
            return;
        }

        if (stabilityResult.Status == WavefunctionStabilityStatus.Inconclusive)
        {
            checkResult.Status = AnomalyCheckStatus.Inconclusive;
            return;
        }

        AnomalyFinding finding = new AnomalyFinding();
        finding.Code = CommonAnomalyCodes.WavefunctionInstability;
        finding.Title = "波函数不稳定";
        finding.Category = AnomalyCategory.Scientific;
        finding.Severity = AnomalySeverity.Error;
        finding.Confidence = 1.0;
        finding.IsBlocking = true;
        finding.RequiresScientificJudgment = true;
        finding.Evidence = new List<AnomalyEvidence>(
            stabilityResult.Evidence);
        finding.Details["outputPath"] =
            stabilityResult.OutputFilePath;
        finding.Details["instabilityKind"] =
            stabilityResult.InstabilityKind;
        checkResult.Status = AnomalyCheckStatus.Finding;
        checkResult.Findings.Add(finding);
    }

    private static string GetExistingOutputPath(AnomalyContext context)
    {
        string? outputPath;

        if (!context.Metadata.TryGetValue(
            AnomalyContextKeys.WavefunctionStabilityOutputPath,
            out outputPath))
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(outputPath)
            || !File.Exists(outputPath))
        {
            return string.Empty;
        }

        return outputPath;
    }

    private static string ResolveOriginalInputPath(CalculationJob job)
    {
        if (File.Exists(job.InputFilePath))
        {
            return job.InputFilePath;
        }

        if (File.Exists(job.SourceInputFilePath))
        {
            return job.SourceInputFilePath;
        }

        return string.Empty;
    }

    private static string ResolveOriginalCheckpointPath(
        CalculationJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.InputFilePath))
        {
            string checkpointPath =
                Path.ChangeExtension(job.InputFilePath, ".chk");

            if (File.Exists(checkpointPath))
            {
                return checkpointPath;
            }
        }

        if (!string.IsNullOrWhiteSpace(job.SourceInputFilePath))
        {
            string checkpointPath =
                Path.ChangeExtension(job.SourceInputFilePath, ".chk");

            if (File.Exists(checkpointPath))
            {
                return checkpointPath;
            }
        }

        if (string.IsNullOrWhiteSpace(job.RunDirectory)
            || !Directory.Exists(job.RunDirectory))
        {
            return string.Empty;
        }

        string[] checkpointFiles = Directory.GetFiles(
            job.RunDirectory,
            "*.chk",
            SearchOption.TopDirectoryOnly);

        if (checkpointFiles.Length == 0)
        {
            return string.Empty;
        }

        string latestPath = checkpointFiles[0];
        DateTime latestWriteTime =
            File.GetLastWriteTimeUtc(latestPath);

        for (int index = 1; index < checkpointFiles.Length; index++)
        {
            DateTime writeTime =
                File.GetLastWriteTimeUtc(checkpointFiles[index]);

            if (writeTime > latestWriteTime)
            {
                latestPath = checkpointFiles[index];
                latestWriteTime = writeTime;
            }
        }

        return latestPath;
    }

    private static bool IsTerminalState(CalculationJobState state)
    {
        return state == CalculationJobState.Completed
            || state == CalculationJobState.Failed
            || state == CalculationJobState.Canceled;
    }

    /// <summary>执行 JSON Skill 请求。</summary>
    protected override Task<AnomalyCheckResult> ExecuteAsync(
        AnomalyCheckRequest request,
        CancellationToken cancellationToken)
    {
        return CheckAsync(request.Context, cancellationToken);
    }

    /// <summary>当前检查始终可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static AnomalyCheckResult CreateBaseResult()
    {
        AnomalyCheckResult result = new AnomalyCheckResult();
        result.Code = DescriptorValue.Code;
        result.ImplementationId = DescriptorValue.ImplementationId;
        result.DisplayName = DescriptorValue.DisplayName;
        result.Category = DescriptorValue.Category;
        result.Mechanism = DescriptorValue.Mechanism;
        result.IsRequired = DescriptorValue.IsRequired;
        return result;
    }

    private static AnomalyCheckDescriptor CreateDescriptor()
    {
        AnomalyCheckDescriptor descriptor =
            new AnomalyCheckDescriptor();
        descriptor.Code = CommonAnomalyCheckCodes.WavefunctionStability;
        descriptor.ImplementationId =
            GaussianSkillIds.WavefunctionStabilityCheck;
        descriptor.Program = Gaussian16ProgramAdapter.ProgramNameValue;
        descriptor.DisplayName = "波函数稳定性检查";
        descriptor.Category = AnomalyCategory.Scientific;
        descriptor.Mechanism =
            AnomalyCheckMechanism.AuxiliaryCalculation;
        descriptor.Version = "1.0.0";
        descriptor.IsRequired = true;
        descriptor.Description =
            "解析 Gaussian 波函数稳定性检查输出。";
        return descriptor;
    }
}
