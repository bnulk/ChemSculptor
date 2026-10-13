using ChemSculptor.Compute;
using ChemSculptor.Conversation;

namespace ChemSculptor.Core.Tests;

/// <summary>优化 Agent 路由和参数解析集成测试。</summary>
public class OptimizationAgentIntegrationTests
{
    /// <summary>验证 Gaussian route 行会路由到优化并解析优化参数。</summary>
    [Fact]
    public async Task RouteLineResolvesOptimizationParameters()
    {
        string text =
            "#p cam-b3lyp/6-31G* opt em=GD3BJ scfcyc=200 nosymm";
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask intent =
            await interpreter.InterpretAsync(text);

        Assert.True(intent.IsSupported);
        Assert.Equal(
            CalculationTaskType.Optimization,
            intent.TaskType);
        Assert.Equal(
            "optimization-complete",
            intent.WorkflowId);

        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        CalculationParameterConversationService parameterService =
            new CalculationParameterConversationService(
                repository,
                new RuleBasedCalculationParameterExtractor(),
                new CalculationParameterResolver());
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.Optimization;
        context.ElectronCount = 10;

        ParameterConversationTurn turn =
            await parameterService.HandleAsync(
                "session-optimization-route",
                text,
                "O 0.0 0.0 0.0",
                CalculationDefaults.CreateDefaultOptimization(),
                context);

        Assert.Equal(
            ParameterConversationTurnStatus.ReadyToSubmit,
            turn.Status);
        Assert.NotNull(turn.Resolution);
        Assert.Equal(
            CalculationTaskType.Optimization,
            turn.Resolution.Spec.TaskType);
        Assert.Equal(
            "200",
            turn.Resolution.Spec.ExtraOptions[
                CalculationOptionKeys.ScfIterationLimit]);
        Assert.Equal(
            "GD3BJ",
            turn.Resolution.Spec.ExtraOptions[
                CalculationOptionKeys.DispersionCorrection]);
        Assert.Equal(
            "Ignore",
            turn.Resolution.Spec.ExtraOptions[
                CalculationOptionKeys.Symmetry]);
    }
}
