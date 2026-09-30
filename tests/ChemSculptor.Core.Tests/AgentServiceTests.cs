using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Conversation;
using ChemSculptor.InputProcessor;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 智能体编排服务测试。
/// </summary>
public class AgentServiceTests
{
    /// <summary>验证不支持的自然语言会返回错误。</summary>
    [Fact]
    public async Task UnsupportedMessageReturnsError()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service = CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-1";
            request.Text = "优化这个分子";
            request.CoordinateText = "O 0.0 0.0 0.0";

            AgentResult result = await service.HandleMessageAsync(request);

            Assert.False(result.IsSupported);
            Assert.Contains("只支持单点计算", result.Error);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证“单点计算”会生成 Gaussian 输入文件。</summary>
    [Fact]
    public async Task SinglePointMessageGeneratesInputFile()
    {
        string root = CreateTemporaryRoot();

        try
        {
            RecordingWorkflowEngine workflowEngine;
            AgentService service = CreateAgentService(root, out workflowEngine);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-2";
            request.Text = "单点计算";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result = await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal("Running", result.Status);
            Assert.Contains("output.log", result.OutputFilePath);
            Assert.Equal(result.JobId, workflowEngine.StartedJobId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingWorkflowEngine workflowEngine)
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = root;

        WorkspaceManager workspace = new WorkspaceManager(options);
        TestQuantumProgramAdapterRegistry adapterRegistry =
            new TestQuantumProgramAdapterRegistry();
        FileCalculationRepository calculationRepository =
            new FileCalculationRepository(workspace);
        RecordingComputeBackend backend = new RecordingComputeBackend();
        workflowEngine = new RecordingWorkflowEngine();

        SinglePointCalculationService singlePointService =
            new SinglePointCalculationService(
                workspace,
                backend,
                adapterRegistry,
                calculationRepository,
                workflowEngine);
        RuleBasedTaskInterpreter interpreter = new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationService conversationService = new ConversationService(interpreter, repository);

        return new AgentService(conversationService, singlePointService);
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
        public string Name
        {
            get { return "recording"; }
        }

        public CalculationJob LastJob { get; private set; } = new CalculationJob();

        public CalculationExecutionContext LastContext { get; private set; } =
            new CalculationExecutionContext();

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            LastJob = job;
            LastContext = context;
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
