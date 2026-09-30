using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>单点计算服务测试。</summary>
public class SinglePointCalculationServiceTests
{
    /// <summary>验证服务可以提交作业、应用覆盖参数并查询作业。</summary>
    [Fact]
    public async Task SubmitsAndQueriesSinglePointJob()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(workspaceOptions);
            TestQuantumProgramAdapterRegistry adapterRegistry =
                new TestQuantumProgramAdapterRegistry();
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);
            RecordingComputeBackend backend = new RecordingComputeBackend();
            RecordingWorkflowEngine workflowEngine =
                new RecordingWorkflowEngine();

            SinglePointCalculationService service =
                new SinglePointCalculationService(
                    workspace,
                    backend,
                    adapterRegistry,
                    repository,
                    workflowEngine);

            CalculationRequest request = new CalculationRequest();
            request.SessionId = "session-service";
            request.Goal = "计算水的单点能";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";
            AddOverride(request, "charge", "1");
            AddOverride(request, "multiplicity", "3");

            SinglePointCalculationSubmissionResult submission =
                await service.SubmitAsync(request);

            Assert.True(submission.Succeeded);
            Assert.NotNull(submission.Job);
            Assert.Equal(CalculationJobState.Running, submission.Job.State);
            Assert.Equal(1, submission.Job.Spec.Charge);
            Assert.Equal(3, submission.Job.Spec.Multiplicity);
            Assert.Equal("session-service", submission.Job.SessionId);
            Assert.Equal("计算水的单点能", submission.Job.Goal);
            Assert.Equal(submission.Job.JobId, workflowEngine.StartedJobId);

            CalculationJob? savedJob =
                await service.GetJobAsync(submission.Job.JobId);
            CalculationResult? savedResult =
                await service.GetResultAsync(submission.Job.JobId);
            CalculationValidationReport? savedValidation =
                await service.GetValidationAsync(submission.Job.JobId);

            Assert.NotNull(savedJob);
            Assert.Equal(CalculationJobState.Running, savedJob.State);
            Assert.Null(savedResult);
            Assert.Null(savedValidation);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证空坐标会被拒绝。</summary>
    [Fact]
    public async Task RejectsEmptyCoordinateText()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(workspaceOptions);
            TestQuantumProgramAdapterRegistry adapterRegistry =
                new TestQuantumProgramAdapterRegistry();
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);
            RecordingComputeBackend backend = new RecordingComputeBackend();
            RecordingWorkflowEngine workflowEngine =
                new RecordingWorkflowEngine();

            SinglePointCalculationService service =
                new SinglePointCalculationService(
                    workspace,
                    backend,
                    adapterRegistry,
                    repository,
                    workflowEngine);

            CalculationRequest request = new CalculationRequest();
            request.CoordinateText = string.Empty;

            SinglePointCalculationSubmissionResult submission =
                await service.SubmitAsync(request);

            Assert.False(submission.Succeeded);
            Assert.Contains("坐标文本不能为空", submission.Error);
            Assert.Null(submission.Job);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证运行中的作业可以取消。</summary>
    [Fact]
    public async Task CancelsRunningCalculation()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;

            WorkspaceManager workspace = new WorkspaceManager(workspaceOptions);
            TestQuantumProgramAdapterRegistry adapterRegistry =
                new TestQuantumProgramAdapterRegistry();
            FileCalculationRepository repository =
                new FileCalculationRepository(workspace);
            RecordingComputeBackend backend = new RecordingComputeBackend();
            RecordingWorkflowEngine workflowEngine =
                new RecordingWorkflowEngine();

            SinglePointCalculationService service =
                new SinglePointCalculationService(
                    workspace,
                    backend,
                    adapterRegistry,
                    repository,
                    workflowEngine);

            CalculationRequest request = new CalculationRequest();
            request.CoordinateText = "O 0.0 0.0 0.0";

            SinglePointCalculationSubmissionResult submission =
                await service.SubmitAsync(request);

            Assert.True(submission.Succeeded);
            Assert.NotNull(submission.Job);

            bool canceled = await service.CancelAsync(submission.Job.JobId);
            CalculationJob? savedJob =
                await service.GetJobAsync(submission.Job.JobId);

            Assert.True(canceled);
            Assert.Equal(submission.Job.JobId, backend.CanceledJobId);
            Assert.NotNull(savedJob);
            Assert.Equal(CalculationJobState.Canceled, savedJob.State);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static void AddOverride(
        CalculationRequest request,
        string name,
        string value)
    {
        CalculationParameter parameter = new CalculationParameter();
        parameter.Name = name;
        parameter.CurrentValue = value;
        request.Overrides.Add(parameter);
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
        public string CanceledJobId { get; private set; } = string.Empty;

        public string Name
        {
            get { return "recording"; }
        }

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(job.JobId);
        }

        public Task<CalculationJobState> GetStatusAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CalculationJobState.Running);
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
            CanceledJobId = job.JobId;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingWorkflowEngine : ISinglePointWorkflowEngine
    {
        public string StartedJobId { get; private set; } = string.Empty;

        public Task<WorkflowRun> StartAsync(
            WorkflowDefinition definition,
            IReadOnlyDictionary<string, string> inputs,
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            StartedJobId = job.JobId;

            WorkflowRun run = new WorkflowRun();
            run.Id = definition.Id;
            run.Definition = definition;
            run.State = WorkflowState.Running;
            return Task.FromResult(run);
        }
    }
}
