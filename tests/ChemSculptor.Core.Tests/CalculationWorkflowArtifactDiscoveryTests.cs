using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.FrequencyAnalysis;
using ChemSculptor.Compute.Gaussian.Nto;
using ChemSculptor.Compute.Gaussian.Tddft;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Core.Tests;

/// <summary>根工作流全过程产物发现测试。</summary>
public class CalculationWorkflowArtifactDiscoveryTests
{
    /// <summary>完整枚举多阶段作业并发现输入、输出、状态和验证证据。</summary>
    [Fact]
    public async Task DiscoversCompleteMultiStageArchive()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            ArchiveTestProgramAdapter adapter =
                new ArchiveTestProgramAdapter();
            QuantumProgramAdapterRegistry adapters =
                new QuantumProgramAdapterRegistry(
                    new[] { (IQuantumProgramAdapter)adapter });
            CalculationJob optimization = CreateJob(
                "job-artifact-opt",
                "workflow-artifact-complete",
                CalculationTaskType.OptimizationFrequency,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.OptimizationFrequency);
            CalculationJob failedRecovery = CreateJob(
                "job-artifact-recovery",
                optimization.RootWorkflowId,
                CalculationTaskType.Optimization,
                CalculationJobState.Failed,
                CalculationWorkflowStageIds.OptimizationRecovery);
            failedRecovery.ParentJobId = optimization.JobId;
            failedRecovery.AttemptNumber = 1;
            CalculationJob canceled = CreateJob(
                "job-artifact-canceled",
                optimization.RootWorkflowId,
                CalculationTaskType.Frequency,
                CalculationJobState.Canceled,
                CalculationWorkflowStageIds.Frequency);

            await PrepareJobAsync(workspace, calculations, optimization, true);
            await PrepareJobAsync(workspace, calculations, failedRecovery, false);
            await PrepareJobAsync(workspace, calculations, canceled, false);

            string optimizationRun = workspace.GetRunDirectory(
                optimization.JobId);
            string optimizationResults = workspace.GetResultDirectory(
                optimization.JobId);
            await File.WriteAllTextAsync(
                Path.Combine(optimizationRun, "optimization.chk"),
                "checkpoint");
            await File.WriteAllTextAsync(
                Path.Combine(optimizationRun, "notes.dat"),
                "unknown persisted evidence");
            Directory.CreateDirectory(
                Path.Combine(optimizationRun, "scratch"));
            await File.WriteAllTextAsync(
                Path.Combine(optimizationRun, "scratch", "matrix.bin"),
                "scratch");
            await File.WriteAllTextAsync(
                Path.Combine(optimizationRun, "scratch", "large.tmp"),
                "temporary");
            Directory.CreateDirectory(
                Path.Combine(optimizationResults, "scientific-data"));
            await File.WriteAllTextAsync(
                Path.Combine(
                    optimizationResults,
                    "scientific-data",
                    "points.json"),
                "{}");

            FileCalculationWorkflowArtifactDiscoveryRepository repository =
                CreateRepository(workspace, adapters);
            CalculationWorkflowArtifactDiscoverySnapshot snapshot =
                await repository.DiscoverAsync(optimization.RootWorkflowId);

            Assert.Equal(3, snapshot.Jobs.Count);
            Assert.Contains(
                snapshot.Jobs,
                job => job.JobId == failedRecovery.JobId
                    && job.State == CalculationJobState.Failed
                    && job.AttemptNumber == 1);
            Assert.Contains(
                snapshot.Jobs,
                job => job.JobId == canceled.JobId
                    && job.State == CalculationJobState.Canceled);
            Assert.Contains(
                snapshot.Files,
                file => file.JobId == optimization.JobId
                    && file.Role == CalculationWorkflowArtifactRole.ProgramInput
                    && file.Status == CalculationWorkflowArtifactDiscoveryStatus.Present);
            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "run/optimization.chk"
                    && file.Role == CalculationWorkflowArtifactRole.RestartState);
            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "run/notes.dat"
                    && file.Role == CalculationWorkflowArtifactRole.Other);
            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "results/scientific-data/points.json"
                    && file.Role == CalculationWorkflowArtifactRole.ScientificData);
            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath.StartsWith(
                        "run/scratch/",
                        StringComparison.Ordinal)
                    && file.IsAggregate
                    && file.Status == CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy);
            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "run/stdout.log"
                    && file.Status == CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy
                    && file.ExclusionCategory == "empty-console-stream");
            Assert.Contains(
                snapshot.Summary.Exclusions,
                exclusion => exclusion.Category == "scratch-cache"
                    && exclusion.FileCount == 2);
            Assert.True(snapshot.Summary.IsComplete);
            Assert.Equal(0, snapshot.Summary.ErrorCount);

            CalculationWorkflowArtifactDiscoverySnapshot? restored =
                await CreateRepository(workspace, adapters)
                    .GetAsync(optimization.RootWorkflowId);
            Assert.NotNull(restored);
            Assert.Equal(
                snapshot.Files.Select(file => file.ArtifactId),
                restored!.Files.Select(file => file.ArtifactId));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>必要文件缺失时给出稳定错误并判定档案不完整。</summary>
    [Fact]
    public async Task ReportsMissingRequiredEvidence()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            ArchiveTestProgramAdapter adapter =
                new ArchiveTestProgramAdapter();
            QuantumProgramAdapterRegistry adapters =
                new QuantumProgramAdapterRegistry(
                    new[] { (IQuantumProgramAdapter)adapter });
            CalculationJob job = CreateJob(
                "job-artifact-missing",
                "workflow-artifact-missing",
                CalculationTaskType.Optimization,
                CalculationJobState.Validated,
                CalculationWorkflowStageIds.Optimization);
            await PrepareJobAsync(workspace, calculations, job, false);
            await File.WriteAllTextAsync(
                workspace.GetJobValidationPath(job.JobId),
                "{}");

            CalculationWorkflowArtifactDiscoverySnapshot snapshot =
                await CreateRepository(workspace, adapters)
                    .DiscoverAsync(job.RootWorkflowId);

            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "results/result.json"
                    && file.Requirement == CalculationWorkflowArtifactRequirement.Required
                    && file.Status == CalculationWorkflowArtifactDiscoveryStatus.Missing);
            Assert.Contains(
                snapshot.Issues,
                issue => issue.Code
                        == CalculationWorkflowArtifactDiscoveryIssueCodes.RequiredEvidenceMissing
                    && issue.LogicalPath == "results/result.json");
            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "results/processing-plan.json"
                    && file.Status == CalculationWorkflowArtifactDiscoveryStatus.NotExpected);
            Assert.False(snapshot.Summary.IsComplete);
            Assert.True(snapshot.Summary.MissingCount > 0);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>重解析点必须显式拒绝，不能跟随到作业目录之外。</summary>
    [Fact]
    public async Task RejectsReparsePointWithoutFollowingIt()
    {
        string root = CreateTemporaryRoot();
        string outside = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-artifact-link",
                "workflow-artifact-link",
                CalculationTaskType.SinglePoint,
                CalculationJobState.Created,
                CalculationWorkflowStageIds.SinglePoint);
            await workspace.EnsureJobWorkspaceAsync(job.JobId);
            job.RunDirectory = workspace.GetRunDirectory(job.JobId);
            await calculations.SaveJobAsync(job);
            string linkPath = Path.Combine(
                workspace.GetRunDirectory(job.JobId),
                "linked-output");

            try
            {
                Directory.CreateSymbolicLink(linkPath, outside);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException
                || exception is IOException
                || exception is PlatformNotSupportedException)
            {
                return;
            }

            ArchiveTestProgramAdapter adapter =
                new ArchiveTestProgramAdapter();
            CalculationWorkflowArtifactDiscoverySnapshot snapshot =
                await CreateRepository(
                        workspace,
                        new QuantumProgramAdapterRegistry(
                            new[] { (IQuantumProgramAdapter)adapter }))
                    .DiscoverAsync(job.RootWorkflowId);

            Assert.Contains(
                snapshot.Files,
                file => file.LogicalPath == "run/linked-output"
                    && file.Status
                        == CalculationWorkflowArtifactDiscoveryStatus.SecurityRejected);
            Assert.Contains(
                snapshot.Issues,
                issue => issue.Code
                    == CalculationWorkflowArtifactDiscoveryIssueCodes
                        .ReparsePointRejected);
        }
        finally
        {
            DeleteTemporaryRoot(root);
            DeleteTemporaryRoot(outside);
        }
    }

    /// <summary>清单运行目录越界时显式报告，不读取越界目录。</summary>
    [Fact]
    public async Task ReportsRunDirectoryOutsideWorkspaceContract()
    {
        string root = CreateTemporaryRoot();
        string outside = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            CalculationJob job = CreateJob(
                "job-artifact-conflict",
                "workflow-artifact-conflict",
                CalculationTaskType.SinglePoint,
                CalculationJobState.Created,
                CalculationWorkflowStageIds.SinglePoint);
            job.RunDirectory = Path.Combine(outside, "run");
            await calculations.SaveJobAsync(job);

            ArchiveTestProgramAdapter adapter =
                new ArchiveTestProgramAdapter();
            CalculationWorkflowArtifactDiscoverySnapshot snapshot =
                await CreateRepository(
                        workspace,
                        new QuantumProgramAdapterRegistry(
                            new[] { (IQuantumProgramAdapter)adapter }))
                    .DiscoverAsync(job.RootWorkflowId);

            Assert.Contains(
                snapshot.Issues,
                issue => issue.Code
                    == CalculationWorkflowArtifactDiscoveryIssueCodes
                        .RunDirectoryConflict);
            Assert.False(snapshot.Summary.IsComplete);
        }
        finally
        {
            DeleteTemporaryRoot(root);
            DeleteTemporaryRoot(outside);
        }
    }

    /// <summary>
    /// Gaussian 程序输入和主要输出必须随作业状态决定是否必要。
    /// </summary>
    [Fact]
    public void GaussianArtifactRequirementsFollowJobState()
    {
        CalculationJob job = CreateJob(
            "job-gaussian-patterns",
            "workflow-gaussian-patterns",
            CalculationTaskType.SinglePoint,
            CalculationJobState.Created,
            CalculationWorkflowStageIds.SinglePoint);
        job.InputFilePath = Path.Combine(
            Path.GetTempPath(),
            job.JobId + ".gjf");
        job.OutputFilePath = Path.Combine(
            Path.GetTempPath(),
            "output.log");
        Gaussian16ProgramAdapter adapter = CreateGaussianAdapter();

        CalculationArtifactDiscoveryContext context =
            new CalculationArtifactDiscoveryContext();
        context.Job = job;
        IReadOnlyList<CalculationArtifactPattern> createdPatterns =
            adapter.GetArtifactPatterns(context);

        Assert.False(FindPattern(createdPatterns, job.JobId + ".gjf").IsRequired);
        Assert.False(FindPattern(createdPatterns, "output.log").IsRequired);

        job.State = CalculationJobState.Running;
        IReadOnlyList<CalculationArtifactPattern> runningPatterns =
            adapter.GetArtifactPatterns(context);

        Assert.True(FindPattern(runningPatterns, job.JobId + ".gjf").IsRequired);
        Assert.False(FindPattern(runningPatterns, "output.log").IsRequired);

        job.State = CalculationJobState.Completed;
        IReadOnlyList<CalculationArtifactPattern> completedPatterns =
            adapter.GetArtifactPatterns(context);

        Assert.True(FindPattern(completedPatterns, job.JobId + ".gjf").IsRequired);
        Assert.True(FindPattern(completedPatterns, "output.log").IsRequired);
    }

    /// <summary>
    /// 必要持久化文件即使按策略排除，也不能降级为可选证据。
    /// </summary>
    [Fact]
    public async Task RequiredEvidenceExcludedRemainsRequired()
    {
        string root = CreateTemporaryRoot();

        try
        {
            WorkspaceManager workspace = CreateWorkspace(root);
            FileCalculationRepository calculations =
                new FileCalculationRepository(workspace);
            ArchiveTestProgramAdapter adapter =
                new ArchiveTestProgramAdapter(
                    true);
            CalculationJob job = CreateJob(
                "job-artifact-required-excluded",
                "workflow-artifact-required-excluded",
                CalculationTaskType.SinglePoint,
                CalculationJobState.Running,
                CalculationWorkflowStageIds.SinglePoint);
            await PrepareJobAsync(
                workspace,
                calculations,
                job,
                false);
            await File.WriteAllTextAsync(
                Path.Combine(
                    workspace.GetRunDirectory(job.JobId),
                    "required.tmp"),
                "must remain required");

            CalculationWorkflowArtifactDiscoverySnapshot snapshot =
                await CreateRepository(
                        workspace,
                        new QuantumProgramAdapterRegistry(
                            new[] { (IQuantumProgramAdapter)adapter }))
                    .DiscoverAsync(job.RootWorkflowId);

            CalculationWorkflowArtifactDiscoveryEntry entry =
                Assert.Single(
                    snapshot.Files,
                    file => file.LogicalPath == "run/required.tmp");
            Assert.Equal(
                CalculationWorkflowArtifactRequirement.Required,
                entry.Requirement);
            Assert.Equal(
                CalculationWorkflowArtifactDiscoveryStatus.ExcludedByPolicy,
                entry.Status);
            Assert.Contains(
                snapshot.Issues,
                issue => issue.Code
                        == CalculationWorkflowArtifactDiscoveryIssueCodes
                            .RequiredEvidenceExcluded
                    && issue.LogicalPath == "run/required.tmp");
            Assert.False(snapshot.Summary.IsComplete);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static async Task PrepareJobAsync(
        WorkspaceManager workspace,
        FileCalculationRepository calculations,
        CalculationJob job,
        bool includeCompletedEvidence)
    {
        await workspace.EnsureJobWorkspaceAsync(job.JobId);
        job.RunDirectory = workspace.GetRunDirectory(job.JobId);
        await calculations.SaveJobAsync(job);
        await File.WriteAllTextAsync(
            workspace.GetJobParametersPath(job.JobId),
            "{}");
        await File.WriteAllTextAsync(
            Path.Combine(
                workspace.GetInputDirectory(job.JobId),
                CalculationWorkspacePaths.JobCoordinatesFileName),
            "O 0 0 0");
        await File.WriteAllTextAsync(
            Path.Combine(
                workspace.GetRunDirectory(job.JobId),
                job.JobId + ".inp"),
            "input");
        await File.WriteAllTextAsync(
            Path.Combine(workspace.GetRunDirectory(job.JobId), "stdout.log"),
            string.Empty);
        await File.WriteAllTextAsync(
            Path.Combine(workspace.GetRunDirectory(job.JobId), "stderr.log"),
            string.Empty);

        if (!includeCompletedEvidence)
        {
            return;
        }

        await File.WriteAllTextAsync(
            Path.Combine(
                workspace.GetRunDirectory(job.JobId),
                CalculationWorkspacePaths.JobOutputFileName),
            "normal termination");
        await File.WriteAllTextAsync(
            workspace.GetJobResultPath(job.JobId),
            "{}");
        await File.WriteAllTextAsync(
            workspace.GetJobValidationPath(job.JobId),
            "{}");
    }

    private static CalculationJob CreateJob(
        string jobId,
        string rootWorkflowId,
        CalculationTaskType taskType,
        CalculationJobState state,
        string stageId)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = jobId;
        job.RootWorkflowId = rootWorkflowId;
        job.EvidenceStageId = stageId;
        job.State = state;
        job.Spec.TaskType = taskType;
        job.Spec.Program = ArchiveTestProgramAdapter.ProgramNameValue;
        return job;
    }

    private static WorkspaceManager CreateWorkspace(string root)
    {
        CalculationWorkspaceOptions options =
            new CalculationWorkspaceOptions();
        options.RootDirectory = root;
        return new WorkspaceManager(options);
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptor-artifact-discovery-"
                + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }

    private static FileCalculationWorkflowArtifactDiscoveryRepository
        CreateRepository(
            WorkspaceManager workspace,
            IQuantumProgramAdapterRegistry adapters)
    {
        return new FileCalculationWorkflowArtifactDiscoveryRepository(
            workspace,
            new FileCalculationWorkflowInventoryRepository(workspace),
            adapters);
    }

    private static Gaussian16ProgramAdapter CreateGaussianAdapter()
    {
        GaussianRouteKeywordBuilder routeKeywordBuilder =
            new GaussianRouteKeywordBuilder();
        return new Gaussian16ProgramAdapter(
            new GaussianInputWriter(routeKeywordBuilder),
            new GaussianOutputParser(),
            new GaussianResultTranslator(),
            new GaussianProcessingPlanTranslator(),
            routeKeywordBuilder,
            new GaussianFrequencyInputWriter(routeKeywordBuilder),
            new GaussianFrequencyOutputParser(),
            new GaussianTddftOutputParser(),
            new GaussianNtoInputWriter(),
            new GaussianNtoOutputParser(),
            new Gaussian16ProgramAdapterOptions());
    }

    private static CalculationArtifactPattern FindPattern(
        IReadOnlyList<CalculationArtifactPattern> patterns,
        string filePattern)
    {
        return Assert.Single(
            patterns,
            pattern => string.Equals(
                pattern.FilePattern,
                filePattern,
                StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ArchiveTestProgramAdapter
        : IQuantumProgramAdapter
    {
        public const string ProgramNameValue = "Archive Test Program";

        private readonly bool _includeRequiredTemporaryArtifact;

        public ArchiveTestProgramAdapter(
            bool includeRequiredTemporaryArtifact = false)
        {
            _includeRequiredTemporaryArtifact =
                includeRequiredTemporaryArtifact;
        }

        public string ProgramName
        {
            get { return ProgramNameValue; }
        }

        public bool CanRun(CalculationSpec spec)
        {
            return spec != null
                && string.Equals(
                    spec.Program,
                    ProgramNameValue,
                    StringComparison.OrdinalIgnoreCase);
        }

        public Task WriteInputAsync(
            CalculationSpec spec,
            CanonicalGeometry geometry,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public string GetInputFileName(string jobId)
        {
            return jobId + ".inp";
        }

        public IReadOnlyList<CalculationArtifactPattern>
            GetArtifactPatterns(
                CalculationArtifactDiscoveryContext context)
        {
            CalculationJob job = context.Job;
            bool requiresInput = job.State != CalculationJobState.Created;
            bool requiresOutput = job.State
                    == CalculationJobState.Completed
                || job.State == CalculationJobState.Parsed
                || job.State == CalculationJobState.Validated;
            List<CalculationArtifactPattern> patterns =
                new List<CalculationArtifactPattern>
            {
                CreatePattern(
                    job.JobId + ".inp",
                    CalculationArtifactKind.Input,
                    "text/plain; charset=utf-8",
                    requiresInput),
                CreatePattern(
                    CalculationWorkspacePaths.JobOutputFileName,
                    CalculationArtifactKind.PrimaryOutput,
                    "text/plain; charset=utf-8",
                    requiresOutput),
                CreatePattern(
                    "*.chk",
                    CalculationArtifactKind.RestartState,
                    "application/octet-stream",
                    false),
                CreatePattern(
                    "stdout.log",
                    CalculationArtifactKind.SupportingOutput,
                    "text/plain; charset=utf-8",
                    false),
                CreatePattern(
                    "stderr.log",
                    CalculationArtifactKind.SupportingOutput,
                    "text/plain; charset=utf-8",
                    false)
            };

            if (_includeRequiredTemporaryArtifact)
            {
                patterns.Add(CreatePattern(
                    "required.tmp",
                    CalculationArtifactKind.PrimaryOutput,
                    "text/plain; charset=utf-8",
                    true));
            }

            return patterns;
        }

        public IReadOnlyList<ProgramParameterTranslation>
            GetParameterTranslations(CalculationSpec spec)
        {
            return new List<ProgramParameterTranslation>();
        }

        public Task PostProcessAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public CalculationExecutionContext BuildExecutionContext(
            CalculationJob job,
            CalculationSpec spec)
        {
            CalculationExecutionContext context =
                new CalculationExecutionContext();
            context.JobId = job.JobId;
            context.RunDirectory = job.RunDirectory;
            context.InputFilePath = job.InputFilePath;
            context.OutputFilePath = job.OutputFilePath;
            return context;
        }

        public Task<CalculationResult> ParseOutputAsync(
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new CalculationResult());
        }

        public Task<ProgramProcessingPlan> TranslateProcessingPlanAsync(
            CalculationJob job,
            CalculationProcessingPlan processingPlan,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProgramProcessingPlan
            {
                JobId = job.JobId,
                Program = ProgramNameValue
            });
        }

        private static CalculationArtifactPattern CreatePattern(
            string filePattern,
            CalculationArtifactKind kind,
            string mediaType,
            bool isRequired)
        {
            return new CalculationArtifactPattern
            {
                FilePattern = filePattern,
                Kind = kind,
                MediaType = mediaType,
                IsRequired = isRequired,
                CanUseForRestart = kind
                    == CalculationArtifactKind.RestartState,
                Scope = CalculationArtifactPatternScope.RunDirectory
            };
        }
    }
}
