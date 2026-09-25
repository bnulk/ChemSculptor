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
            AgentService service = CreateAgentService(root);

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
            AgentService service = CreateAgentService(root);

            AgentRequest request = new AgentRequest();
            request.SessionId = "session-2";
            request.Text = "单点计算";
            request.CoordinateText =
                "O 0.000000 0.000000 0.117300\n" +
                "H 0.000000 0.757200 -0.469200\n" +
                "H 0.000000 -0.757200 -0.469200";

            AgentResult result = await service.HandleMessageAsync(request);

            Assert.True(result.IsSupported);
            Assert.Equal("InputGenerated", result.Status);
            Assert.True(File.Exists(result.InputFilePath));

            string text = await File.ReadAllTextAsync(result.InputFilePath);
            Assert.Contains("#p CAM-B3LYP/6-31G* SP", text);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static AgentService CreateAgentService(string root)
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();
        options.RootDirectory = root;

        WorkspaceManager workspace = new WorkspaceManager(options);
        GaussianInputWriter inputWriter = new GaussianInputWriter();
        GeometryTextParser geometryParser = new GeometryTextParser();

        SinglePointCalculationExecutor executor =
            new SinglePointCalculationExecutor(geometryParser, workspace, inputWriter);
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
}
