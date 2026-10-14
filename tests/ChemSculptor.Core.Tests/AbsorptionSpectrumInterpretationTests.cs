using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>吸收光谱任务解释测试。</summary>
public class AbsorptionSpectrumInterpretationTests
{
    /// <summary>验证“吸收光谱”默认识别为优化后 TDDFT。</summary>
    [Fact]
    public async Task InterpretsDefaultAbsorptionSpectrumMode()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask intent =
            await interpreter.InterpretAsync("吸收光谱");

        Assert.True(intent.IsSupported);
        Assert.Equal(CalculationTaskType.TdDft, intent.TaskType);
        Assert.Equal("absorption-spectrum", intent.WorkflowId);
        Assert.Equal(
            AbsorptionSpectrumMode.OptimizeThenTddft,
            intent.AbsorptionSpectrumMode);
    }

    /// <summary>验证“不优化”会切换为直接 TDDFT 模式。</summary>
    [Fact]
    public async Task InterpretsTddftOnlyAbsorptionSpectrumMode()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask intent =
            await interpreter.InterpretAsync("紫外，不优化");

        Assert.True(intent.IsSupported);
        Assert.Equal(
            AbsorptionSpectrumMode.TddftOnly,
            intent.AbsorptionSpectrumMode);
    }

    /// <summary>验证冻结非氢原子会切换为约束优化后 TDDFT。</summary>
    [Fact]
    public async Task InterpretsConstrainedAbsorptionSpectrumMode()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask intent =
            await interpreter.InterpretAsync(
                "吸收光谱，冻结非氢原子");

        Assert.True(intent.IsSupported);
        Assert.Equal(
            AbsorptionSpectrumMode
                .ConstrainedOptimizationThenTddft,
            intent.AbsorptionSpectrumMode);
    }

    /// <summary>验证不优化和冻结约束冲突时不会自动执行。</summary>
    [Fact]
    public async Task RejectsConflictingAbsorptionSpectrumMode()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask intent =
            await interpreter.InterpretAsync(
                "吸收光谱，不优化，冻结非氢原子");

        Assert.False(intent.IsSupported);
        Assert.Contains(
            intent.Diagnostics,
            diagnostic => diagnostic.Contains("不能同时使用"));
    }
}
