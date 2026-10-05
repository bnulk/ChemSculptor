using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Gaussian;
using ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.AnomalyWorkflow;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 波函数稳定性检查测试。</summary>
public class GaussianWavefunctionStabilityTests
{
    /// <summary>验证稳定输出被翻译为稳定状态。</summary>
    [Fact]
    public async Task ParsesStableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "stable.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is stable under the perturbations considered.");

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Stable,
                result.Status);
            Assert.Single(result.Evidence);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证不稳定输出被翻译为不稳定状态和类型。</summary>
    [Fact]
    public async Task ParsesUnstableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unstable.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Unstable,
                result.Status);
            Assert.Equal("internal", result.InstabilityKind);
            Assert.Single(result.Evidence);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证稳定性优化后已稳定时仍保留最初的不稳定发现。</summary>
    [Fact]
    public async Task KeepsInitialInstabilityAfterOptimizationReportsStable()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "optimized.log");

        try
        {
            string text =
                "The wavefunction has an RHF -> UHF instability.\n" +
                "The wavefunction is stable under the perturbations considered.";
            await File.WriteAllTextAsync(path, text);

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Unstable,
                result.Status);
            Assert.Equal("RHF-to-UHF", result.InstabilityKind);
            Assert.Equal(2, result.Evidence.Count);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证没有识别结论时返回可区分的状态。</summary>
    [Fact]
    public async Task ReturnsInconclusiveWhenStatementIsMissing()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unknown.log");

        try
        {
            await File.WriteAllTextAsync(path, "No stability result here.");

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Inconclusive,
                result.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证不稳定检查会返回 Finding 和科学异常。</summary>
    [Fact]
    public async Task CheckReturnsFindingForUnstableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unstable-detector.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            GaussianWavefunctionStabilityCheckSkill check =
                CreateCheckSkill(root, new RecordingComputeBackend());
            AnomalyContext context = CreateGaussianContext(path);

            AnomalyCheckResult checkResult =
                await check.CheckAsync(context);

            Assert.Equal(
                AnomalyCheckStatus.Finding,
                checkResult.Status);
            Assert.Single(checkResult.Findings);
            Assert.Equal(
                CommonAnomalyCodes.WavefunctionInstability,
                checkResult.Findings[0].Code);
            Assert.Equal(
                AnomalyCategory.Scientific,
                checkResult.Findings[0].Category);
            Assert.True(
                checkResult.Findings[0].RequiresScientificJudgment);
            Assert.True(checkResult.Findings[0].IsBlocking);
            Assert.Equal(
                AnomalyCheckMechanism.AuxiliaryCalculation,
                check.Descriptor.Mechanism);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证稳定输出返回 Passed。</summary>
    [Fact]
    public async Task CheckReturnsPassedForStableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "stable-check.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is stable under the perturbations considered.");

            GaussianWavefunctionStabilityCheckSkill check =
                CreateCheckSkill(root, new RecordingComputeBackend());
            AnomalyContext context = CreateGaussianContext(path);

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(AnomalyCheckStatus.Passed, result.Status);
            Assert.Empty(result.Findings);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证缺少输出时返回 Skipped 和跳过原因。</summary>
    [Fact]
    public async Task CheckReturnsSkippedWithoutOutput()
    {
        string root = CreateTemporaryRoot();

        try
        {
            GaussianWavefunctionStabilityCheckSkill check =
                CreateCheckSkill(
                    root,
                    new RecordingComputeBackend());
            AnomalyContext context = CreateGaussianContext(string.Empty);

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(AnomalyCheckStatus.Skipped, result.Status);
            Assert.Contains("没有可用", result.SkippedReason);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证无法判断时返回 Inconclusive。</summary>
    [Fact]
    public async Task CheckReturnsInconclusiveWithoutStatement()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "inconclusive-check.log");

        try
        {
            await File.WriteAllTextAsync(path, "No stability conclusion.");

            GaussianWavefunctionStabilityCheckSkill check =
                CreateCheckSkill(root, new RecordingComputeBackend());
            AnomalyContext context = CreateGaussianContext(path);

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(
                AnomalyCheckStatus.Inconclusive,
                result.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证通用 Skill 选择 Gaussian 专用 Skill。</summary>
    [Fact]
    public async Task GenericSkillDispatchesToGaussianCheck()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "generic-dispatch.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            AnomalyProviderRegistry registry =
                new AnomalyProviderRegistry();
            RecordingComputeBackend backend =
                new RecordingComputeBackend();
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = Path.Combine(
                root,
                "generic-workspace");
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileAnomalyRepository repository =
                new FileAnomalyRepository(workspace);
            GaussianWavefunctionStabilityCheckSkill gaussianCheck =
                CreateCheckSkill(workspace, backend);
            registry.RegisterCheck(gaussianCheck);

            WavefunctionStabilityCheckSkill genericSkill =
                new WavefunctionStabilityCheckSkill(
                    registry,
                    repository);
            AnomalyCheckRequest request = new AnomalyCheckRequest();
            request.CheckCode =
                CommonAnomalyCheckCodes.WavefunctionStability;
            request.Context = CreateGaussianContext(path);

            TaskRequest taskRequest = new TaskRequest();
            taskRequest.WorkflowId = "workflow-generic";
            taskRequest.NodeId = "stability-check";
            taskRequest.Inputs[
                JsonSkill<AnomalyCheckRequest, AnomalyCheckResult>.RequestKey] =
                SkillJson.Serialize(request);

            TaskResult taskResult =
                await genericSkill.ExecuteAsync(taskRequest);

            if (string.IsNullOrWhiteSpace(taskResult.Output))
            {
                throw new InvalidOperationException(
                    "通用 Skill 没有返回检查结果。");
            }

            AnomalyCheckResult checkResult =
                SkillJson.Deserialize<AnomalyCheckResult>(
                    taskResult.Output);

            Assert.True(taskResult.Succeeded);
            Assert.Equal(
                AnomalyCheckStatus.Finding,
                checkResult.Status);
            Assert.Equal(
                GaussianSkillIds.WavefunctionStabilityCheck,
                checkResult.ImplementationId);
            Assert.False(
                string.IsNullOrWhiteSpace(checkResult.AnomalyRecordId));

            AnomalyRecord? record =
                await repository.GetAsync(
                    request.Context.Job!.JobId,
                    checkResult.AnomalyRecordId);

            Assert.NotNull(record);
            Assert.Equal(AnomalyRecordStatus.Open, record.Status);
            Assert.Single(record.Checks);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证没有现成输出时会执行派生稳定性检查作业。</summary>
    [Fact]
    public async Task CheckRunsAuxiliaryJobWhenOutputIsMissing()
    {
        string root = CreateTemporaryRoot();
        string sourcePath = Path.Combine(root, "original.gjf");

        try
        {
            string sourceText =
                "%chk=original.chk\n" +
                "%mem=4GB\n" +
                "%nprocshared=4\n" +
                "\n" +
                "#p CAM-B3LYP/6-31G* SP scfcyc=200\n" +
                "\n" +
                "water single point\n" +
                "\n" +
                "0 1\n" +
                "O 0.0 0.0 0.0\n" +
                "H 0.0 0.0 1.0\n" +
                "H 0.0 1.0 0.0\n";
            await File.WriteAllTextAsync(sourcePath, sourceText);
            string sourceCheckpointPath =
                Path.ChangeExtension(sourcePath, ".chk");
            await File.WriteAllBytesAsync(
                sourceCheckpointPath,
                new byte[] { 1, 2, 3, 4 });

            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = Path.Combine(
                root,
                "workspace");
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);

            RecordingComputeBackend backend =
                new RecordingComputeBackend();
            GaussianWavefunctionStabilityCheckSkill check =
                CreateCheckSkill(
                    workspace,
                    backend);

            CalculationJob originalJob = new CalculationJob();
            originalJob.JobId = "job-original";
            originalJob.InputFilePath = sourcePath;
            originalJob.SourceInputFilePath = sourcePath;
            originalJob.Spec =
                CalculationDefaults.CreateDefaultSinglePoint();

            AnomalyContext context = new AnomalyContext();
            context.Job = originalJob;

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(AnomalyCheckStatus.Finding, result.Status);
            Assert.False(string.IsNullOrWhiteSpace(result.AuxiliaryJobId));
            Assert.Equal(
                result.AuxiliaryJobId,
                backend.SubmittedJobId);

            string auxiliaryInputPath = Path.Combine(
                workspace.GetRunDirectory(result.AuxiliaryJobId),
                result.AuxiliaryJobId + ".gjf");
            string auxiliaryInput =
                await File.ReadAllTextAsync(auxiliaryInputPath);

            Assert.Contains(
                "guess=read geom=check stable",
                auxiliaryInput);
            Assert.Contains("scfcyc=200", auxiliaryInput);
            Assert.DoesNotContain(" SP", auxiliaryInput);
            Assert.DoesNotContain(
                "O 0.0 0.0 0.0",
                auxiliaryInput);
            Assert.Contains(
                "%chk=" + result.AuxiliaryJobId + ".chk",
                auxiliaryInput);

            string auxiliaryCheckpointPath = Path.Combine(
                workspace.GetRunDirectory(result.AuxiliaryJobId),
                result.AuxiliaryJobId + ".chk");

            Assert.True(File.Exists(auxiliaryCheckpointPath));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 ONIOM 输入进入检查节点后返回 Skipped。</summary>
    [Fact]
    public async Task CheckSkipsOniomInput()
    {
        string root = CreateTemporaryRoot();
        string sourcePath = Path.Combine(root, "oniom.gjf");

        try
        {
            string sourceText =
                "%chk=oniom.chk\n" +
                "%mem=4GB\n" +
                "%nprocshared=4\n" +
                "\n" +
                "#p CAM-B3LYP/6-31G* ONIOM(UB3LYP/6-31G*:UFF) SP\n" +
                "\n" +
                "oniom single point\n" +
                "\n" +
                "0 1\n" +
                "O 0.0 0.0 0.0\n";
            await File.WriteAllTextAsync(sourcePath, sourceText);
            await File.WriteAllBytesAsync(
                Path.ChangeExtension(sourcePath, ".chk"),
                new byte[] { 1, 2, 3, 4 });

            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = Path.Combine(
                root,
                "workspace");
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);

            GaussianWavefunctionStabilityCheckSkill check =
                CreateCheckSkill(
                    workspace,
                    new RecordingComputeBackend());

            CalculationJob originalJob = new CalculationJob();
            originalJob.JobId = "job-oniom";
            originalJob.InputFilePath = sourcePath;
            originalJob.SourceInputFilePath = sourcePath;
            originalJob.Spec =
                CalculationDefaults.CreateDefaultSinglePoint();

            AnomalyContext context = new AnomalyContext();
            context.Job = originalJob;

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(AnomalyCheckStatus.Skipped, result.Status);
            Assert.Contains("ONIOM", result.SkippedReason);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static GaussianWavefunctionStabilityCheckSkill CreateCheckSkill(
        string root,
        RecordingComputeBackend backend)
    {
        CalculationWorkspaceOptions options =
            new CalculationWorkspaceOptions();
        options.RootDirectory = Path.Combine(root, "workspace");
        WorkspaceManager workspace = new WorkspaceManager(options);
        return CreateCheckSkill(workspace, backend);
    }

    private static GaussianWavefunctionStabilityCheckSkill CreateCheckSkill(
        WorkspaceManager workspace,
        RecordingComputeBackend backend)
    {
        FileCalculationRepository repository =
            new FileCalculationRepository(workspace);

        return new GaussianWavefunctionStabilityCheckSkill(
            new GaussianWavefunctionStabilityParser(),
            new GaussianWavefunctionStabilityInputWriter(),
            workspace,
            backend,
            new TestQuantumProgramAdapterRegistry(),
            repository);
    }

    private static AnomalyContext CreateGaussianContext(string outputPath)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-stability";
        job.Spec = CalculationDefaults.CreateDefaultSinglePoint();

        AnomalyContext context = new AnomalyContext();
        context.Job = job;

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            context.Metadata[
                AnomalyContextKeys.WavefunctionStabilityOutputPath] =
                outputPath;
        }

        return context;
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private sealed class RecordingComputeBackend : IComputeBackend
    {
        public string SubmittedJobId { get; private set; } = string.Empty;

        public string Name
        {
            get { return "recording"; }
        }

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            SubmittedJobId = job.JobId;
            File.WriteAllText(
                job.OutputFilePath,
                "The wavefunction is unstable with respect to internal perturbations.");
            return Task.FromResult(job.JobId);
        }

        public Task<CalculationJobState> GetStatusAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                CalculationJobState.Completed);
        }

        public Task FetchArtifactsAsync(
            CalculationJob job,
            string localDirectory,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task CancelAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
