using ChemSculptor.Compute;
using ChemSculptor.Conversation;

namespace ChemSculptor.Core.Tests;

/// <summary>参数问询闭环测试。</summary>
public class CalculationParameterConversationServiceTests
{
    /// <summary>验证第一轮问题会保存，第二轮回答后准备提交。</summary>
    [Fact]
    public async Task ContinuesPendingParameterConversation()
    {
        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        CalculationParameterConversationService service =
            CreateService(repository);

        ParameterConversationTurn first =
            await service.HandleAsync(
                "session-loop",
                "单点计算，对称性",
                "O 0.0 0.0 0.0",
                CalculationDefaults.CreateDefaultSinglePoint(),
                CreateContext(16));

        Assert.Equal(
            ParameterConversationTurnStatus.NeedsClarification,
            first.Status);
        Assert.NotNull(first.PendingRequest);
        Assert.Contains(
            first.PendingRequest.MissingParameters,
            key => key == CalculationOptionKeys.Symmetry);

        ParameterConversationTurn second =
            await service.HandleAsync(
                "session-loop",
                "关闭对称性",
                string.Empty,
                CalculationDefaults.CreateDefaultSinglePoint(),
                CreateContext(16));

        Assert.Equal(
            ParameterConversationTurnStatus.ReadyToSubmit,
            second.Status);
        Assert.NotNull(second.Resolution);
        Assert.Equal(
            "Ignore",
            second.Resolution.Spec.ExtraOptions[
                CalculationOptionKeys.Symmetry]);

        ConversationSession? session =
            await repository.GetSessionAsync("session-loop");
        Assert.NotNull(session);
        Assert.Null(session.PendingParameterRequest);
    }

    /// <summary>验证冲突回答后可以继续完成参数收集。</summary>
    [Fact]
    public async Task ResolvesConflictInSecondTurn()
    {
        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        CalculationParameterConversationService service =
            CreateService(repository);

        ParameterConversationTurn first =
            await service.HandleAsync(
                "session-conflict",
                "nosymm 和 使用对称性",
                "O 0.0 0.0 0.0",
                CalculationDefaults.CreateDefaultSinglePoint(),
                CreateContext(16));

        Assert.Equal(
            ParameterConversationTurnStatus.NeedsClarification,
            first.Status);
        Assert.NotNull(first.PendingRequest);
        Assert.Contains(
            first.PendingRequest.ConflictParameters,
            key => key == CalculationOptionKeys.Symmetry);

        ParameterConversationTurn second =
            await service.HandleAsync(
                "session-conflict",
                "使用对称性",
                string.Empty,
                CalculationDefaults.CreateDefaultSinglePoint(),
                CreateContext(16));

        Assert.Equal(
            ParameterConversationTurnStatus.ReadyToSubmit,
            second.Status);
        Assert.NotNull(second.Resolution);
        Assert.Equal(
            "Use",
            second.Resolution.Spec.ExtraOptions[
                CalculationOptionKeys.Symmetry]);
    }

    /// <summary>验证取消和重新开始都会清除待回答请求。</summary>
    [Fact]
    public async Task ClearsPendingRequestOnCancelAndRestart()
    {
        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        CalculationParameterConversationService service =
            CreateService(repository);

        await service.HandleAsync(
            "session-cancel",
            "单点计算，对称性",
            "O 0.0 0.0 0.0",
            CalculationDefaults.CreateDefaultSinglePoint(),
            CreateContext(16));

        ParameterConversationTurn canceled =
            await service.HandleAsync(
                "session-cancel",
                "取消",
                string.Empty,
                CalculationDefaults.CreateDefaultSinglePoint(),
                CreateContext(16));

        Assert.Equal(
            ParameterConversationTurnStatus.Canceled,
            canceled.Status);
        ConversationSession? canceledSession =
            await repository.GetSessionAsync("session-cancel");
        Assert.NotNull(canceledSession);
        Assert.Null(canceledSession.PendingParameterRequest);

        await service.HandleAsync(
            "session-restart",
            "单点计算，对称性",
            "O 0.0 0.0 0.0",
            CalculationDefaults.CreateDefaultSinglePoint(),
            CreateContext(16));

        ParameterConversationTurn restarted =
            await service.HandleAsync(
                "session-restart",
                "重新开始",
                string.Empty,
                CalculationDefaults.CreateDefaultSinglePoint(),
                CreateContext(16));

        Assert.Equal(
            ParameterConversationTurnStatus.Restarted,
            restarted.Status);
        ConversationSession? restartedSession =
            await repository.GetSessionAsync("session-restart");
        Assert.NotNull(restartedSession);
        Assert.Null(restartedSession.PendingParameterRequest);
    }

    private static CalculationParameterConversationService
        CreateService(
            InMemoryConversationRepository repository)
    {
        return new CalculationParameterConversationService(
            repository,
            new RuleBasedCalculationParameterExtractor(),
            new CalculationParameterResolver());
    }

    private static CalculationParameterResolutionContext CreateContext(
        int electronCount)
    {
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.SinglePoint;
        context.ElectronCount = electronCount;
        return context;
    }
}
