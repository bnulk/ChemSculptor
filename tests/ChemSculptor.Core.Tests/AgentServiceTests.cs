using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Conversation;
using ChemSculptor.InputProcessor;

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
            RecordingComputeBackend backend;
            AgentService service = CreateAgentService(root, out backend);

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
            RecordingComputeBackend backend;
            AgentService service = CreateAgentService(root, out backend);

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
            Assert.True(File.Exists(result.InputFilePath));
            Assert.Contains("output.log", result.OutputFilePath);
            Assert.Equal("g16", backend.LastContext.ExecutablePath);
            Assert.Equal(2, backend.LastContext.Arguments.Count);

            string text = await File.ReadAllTextAsync(result.InputFilePath);
            Assert.Contains("#p CAM-B3LYP/6-31G* SP", text);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static AgentService CreateAgentService(
        string root,
        out RecordingComputeBackend backend)
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = root;

        WorkspaceManager workspace = new WorkspaceManager(options);
        GaussianInputWriter inputWriter = new GaussianInputWriter();
        Gaussian16ProgramAdapterOptions programOptions =
            Gaussian16ProgramAdapterOptions.CreateDefault();
        Gaussian16ProgramAdapter programAdapter =
            new Gaussian16ProgramAdapter(inputWriter, programOptions);
        GeometryTextParser geometryParser = new GeometryTextParser();
        backend = new RecordingComputeBackend();

        SinglePointCalculationExecutor executor =
            new SinglePointCalculationExecutor(
                geometryParser,
                workspace,
                programAdapter,
                backend);
        RuleBasedTaskInterpreter interpreter = new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationService conversationService = new ConversationService(interpreter, repository);

        return new AgentService(conversationService, executor);
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
}
