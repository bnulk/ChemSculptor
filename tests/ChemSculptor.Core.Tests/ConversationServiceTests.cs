using ChemSculptor.Compute;
using ChemSculptor.Conversation;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 会话服务与规则解释器测试。
/// </summary>
public class ConversationServiceTests
{
    /// <summary>验证“单点计算”被解释为单点任务。</summary>
    [Fact]
    public async Task SinglePointMessageIsRecognized()
    {
        RuleBasedTaskInterpreter interpreter = new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationService service = new ConversationService(interpreter, repository);

        ConversationRequest request = new ConversationRequest();
        request.SessionId = "session-1";
        request.Text = "单点计算";

        ConversationReply reply = await service.HandleMessageAsync(request);

        Assert.True(reply.Intent.IsSupported);
        Assert.Equal(CalculationTaskType.SinglePoint, reply.Intent.TaskType);
        Assert.Equal(
            ElectronicStateObjective.GroundState,
            reply.Intent.ElectronicStateObjective);
        Assert.Contains("单点计算", reply.ReplyMessage);
    }

    /// <summary>验证明确指定三重态时记录目标自旋多重度。</summary>
    [Fact]
    public async Task TripletSinglePointMessageIsRecognized()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        ConversationService service =
            new ConversationService(interpreter, repository);

        ConversationRequest request = new ConversationRequest();
        request.SessionId = "session-triplet";
        request.Text = "三重态单点计算";

        ConversationReply reply =
            await service.HandleMessageAsync(request);

        Assert.True(reply.Intent.IsSupported);
        Assert.Equal(
            ElectronicStateObjective.TargetSpinState,
            reply.Intent.ElectronicStateObjective);
        Assert.Equal(3, reply.Intent.TargetMultiplicity);
    }

    /// <summary>验证激发态目标可以被识别为暂未实现的框架状态。</summary>
    [Fact]
    public async Task ExcitedStateSinglePointMessageIsMarked()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        ConversationService service =
            new ConversationService(interpreter, repository);

        ConversationRequest request = new ConversationRequest();
        request.SessionId = "session-excited";
        request.Text = "S1 激发态单点计算";

        ConversationReply reply =
            await service.HandleMessageAsync(request);

        Assert.True(reply.Intent.IsSupported);
        Assert.Equal(
            ElectronicStateObjective.TargetExcitedState,
            reply.Intent.ElectronicStateObjective);
        Assert.Equal("S1", reply.Intent.TargetStateLabel);
    }

    /// <summary>验证未知任务被拒绝。</summary>
    [Fact]
    public async Task UnknownMessageIsRejected()
    {
        RuleBasedTaskInterpreter interpreter = new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationService service = new ConversationService(interpreter, repository);

        ConversationRequest request = new ConversationRequest();
        request.SessionId = "session-2";
        request.Text = "优化这个分子";

        ConversationReply reply = await service.HandleMessageAsync(request);

        Assert.False(reply.Intent.IsSupported);
        Assert.Contains("只支持单点计算", reply.ReplyMessage);
    }

    /// <summary>验证用户消息和智能体回复会写入会话历史。</summary>
    [Fact]
    public async Task MessagesAreStoredInSession()
    {
        RuleBasedTaskInterpreter interpreter = new RuleBasedTaskInterpreter();
        InMemoryConversationRepository repository = new InMemoryConversationRepository();
        ConversationService service = new ConversationService(interpreter, repository);

        ConversationRequest request = new ConversationRequest();
        request.SessionId = "session-3";
        request.Text = "单点计算";

        await service.HandleMessageAsync(request);
        IReadOnlyList<ConversationMessage> messages =
            await repository.GetMessagesAsync("session-3");

        Assert.Equal(2, messages.Count);
        Assert.Equal(ConversationMessageRole.User, messages[0].Role);
        Assert.Equal(ConversationMessageRole.Agent, messages[1].Role);
    }
}
