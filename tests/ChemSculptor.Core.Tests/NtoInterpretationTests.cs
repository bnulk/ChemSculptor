using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>NTO 自然语言任务解释测试。</summary>
public class NtoInterpretationTests
{
    /// <summary>验证出现 NTO 时默认识别为 NTO 任务。</summary>
    [Fact]
    public async Task NtoMessageUsesDefaultSelection()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();

        InterpretedTask intent =
            await interpreter.InterpretAsync(
                "请做 NTO 分析");

        Assert.True(intent.IsSupported);
        Assert.Equal(CalculationTaskType.Nto, intent.TaskType);
        Assert.Equal("nto", intent.WorkflowId);
        Assert.Equal(
            NtoSelectionMode.Default,
            intent.NtoSelectionMode);
        Assert.Null(intent.NtoWavelengthMinNm);
        Assert.Null(intent.NtoWavelengthMaxNm);
    }

    /// <summary>验证两个数字由短横连接时按纳米波长范围解释。</summary>
    [Theory]
    [InlineData("计算 NTO 300-500")]
    [InlineData("计算 NTO300-500")]
    public async Task NtoHyphenatedNumbersUseNanometerRange(
        string text)
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();

        InterpretedTask intent =
            await interpreter.InterpretAsync(text);

        Assert.True(intent.IsSupported);
        Assert.Equal(
            NtoSelectionMode.WavelengthRange,
            intent.NtoSelectionMode);
        Assert.Equal(300.0, intent.NtoWavelengthMinNm);
        Assert.Equal(500.0, intent.NtoWavelengthMaxNm);
    }

    /// <summary>验证带单位和中文连接词的波长范围。</summary>
    [Fact]
    public async Task NtoRangeAcceptsUnitAndChineseSeparator()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();

        InterpretedTask intent =
            await interpreter.InterpretAsync(
                "NTO 300 nm 至 500 nm");

        Assert.True(intent.IsSupported);
        Assert.Equal(
            NtoSelectionMode.WavelengthRange,
            intent.NtoSelectionMode);
        Assert.Equal(300.0, intent.NtoWavelengthMinNm);
        Assert.Equal(500.0, intent.NtoWavelengthMaxNm);
    }

    /// <summary>验证 NTO 前的“全部”限定语选择全部激发态。</summary>
    [Theory]
    [InlineData("全部 NTO")]
    [InlineData("全部吸收的 NTO")]
    [InlineData("全部波长的 NTO")]
    public async Task AllQualifierBeforeNtoUsesAllMode(
        string text)
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();

        InterpretedTask intent =
            await interpreter.InterpretAsync(text);

        Assert.True(intent.IsSupported);
        Assert.Equal(
            NtoSelectionMode.All,
            intent.NtoSelectionMode);
    }

    /// <summary>验证 NTO 后面的“全部”不会改变选择模式。</summary>
    [Fact]
    public async Task AllQualifierAfterNtoDoesNotChangeMode()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();

        InterpretedTask intent =
            await interpreter.InterpretAsync(
                "NTO，全部波长");

        Assert.True(intent.IsSupported);
        Assert.Equal(
            NtoSelectionMode.Default,
            intent.NtoSelectionMode);
    }

    /// <summary>验证反向波长范围会被明确拒绝。</summary>
    [Fact]
    public async Task ReversedNtoWavelengthRangeIsRejected()
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();

        InterpretedTask intent =
            await interpreter.InterpretAsync(
                "NTO 500-300");

        Assert.False(intent.IsSupported);
        Assert.Contains(
            intent.Diagnostics,
            diagnostic =>
                diagnostic.Contains("波长范围无效"));
    }
}
