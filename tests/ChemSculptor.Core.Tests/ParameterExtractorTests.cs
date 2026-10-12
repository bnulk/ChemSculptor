using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>第十二步要求的参数提取测试。</summary>
public class ParameterExtractorTests
{
    /// <summary>验证 em=GD3BJ 会映射为显式 GD3BJ。</summary>
    [Fact]
    public async Task ExtractsDispersionFromGd3bjAlias()
    {
        CalculationParameterExtractionResult result =
            await ExtractAsync("em=GD3BJ");
        CalculationParameterIntent intent =
            FindSingle(
                result,
                CalculationOptionKeys.DispersionCorrection);

        Assert.Equal("GD3BJ", intent.Value);
        Assert.Equal(
            CalculationParameterIntentState.ExplicitValue,
            intent.State);
    }

    /// <summary>验证 nosymm 会映射为 Ignore。</summary>
    [Fact]
    public async Task ExtractsNosymmAsSymmetryFalse()
    {
        CalculationParameterExtractionResult result =
            await ExtractAsync("nosymm");
        CalculationParameterIntent intent =
            FindSingle(result, CalculationOptionKeys.Symmetry);

        Assert.Equal("Ignore", intent.Value);
        Assert.Equal(
            CalculationParameterIntentState.ExplicitValue,
            intent.State);
    }

    /// <summary>验证 SCF 迭代上限可以被提取。</summary>
    [Fact]
    public async Task ExtractsScfIterationLimit()
    {
        CalculationParameterExtractionResult result =
            await ExtractAsync("SCF 最多 300");
        CalculationParameterIntent intent =
            FindSingle(
                result,
                CalculationOptionKeys.ScfIterationLimit);

        Assert.Equal("300", intent.Value);
    }

    /// <summary>验证对称性冲突不会被自动合并。</summary>
    [Fact]
    public async Task DetectsConflictingSymmetry()
    {
        CalculationParameterExtractionResult result =
            await ExtractAsync("nosymm 和 使用对称性");
        List<CalculationParameterIntent> intents =
            FindAll(result, CalculationOptionKeys.Symmetry);

        Assert.Equal(2, intents.Count);
        Assert.Contains(
            intents,
            intent => intent.Value == "Ignore");
        Assert.Contains(
            intents,
            intent => intent.Value == "Use");
    }

    /// <summary>验证未知 raw keyword=value 会被标记为不支持。</summary>
    [Fact]
    public async Task RejectsUnknownRawRouteKeyword()
    {
        CalculationParameterExtractionResult result =
            await ExtractAsync("unknown-keyword=value");

        Assert.Contains(
            result.Intents,
            intent =>
                intent.State
                    == CalculationParameterIntentState.Unsupported
                && intent.RawText == "unknown-keyword=value");
    }

    /// <summary>验证 #p / #n / #t / # 会映射为输出信息量。</summary>
    [Fact]
    public async Task ExtractsRoutePrintLevelAliases()
    {
        CalculationParameterExtractionResult verbose =
            await ExtractAsync("#p");
        CalculationParameterExtractionResult normal =
            await ExtractAsync("#n");
        CalculationParameterExtractionResult terse =
            await ExtractAsync("#t");
        CalculationParameterExtractionResult defaultLevel =
            await ExtractAsync("#");

        Assert.Equal(
            "Verbose",
            FindSingle(
                verbose,
                CalculationOptionKeys.RoutePrintLevel).Value);
        Assert.Equal(
            "Normal",
            FindSingle(
                normal,
                CalculationOptionKeys.RoutePrintLevel).Value);
        Assert.Equal(
            "Terse",
            FindSingle(
                terse,
                CalculationOptionKeys.RoutePrintLevel).Value);
        Assert.Equal(
            "Default",
            FindSingle(
                defaultLevel,
                CalculationOptionKeys.RoutePrintLevel).Value);
    }

    private static async Task<CalculationParameterExtractionResult>
        ExtractAsync(string text)
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionContext context =
            new CalculationParameterExtractionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.SinglePoint;
        return await extractor.ExtractAsync(text, context);
    }

    private static CalculationParameterIntent FindSingle(
        CalculationParameterExtractionResult result,
        string key)
    {
        List<CalculationParameterIntent> intents =
            FindAll(result, key);

        Assert.Single(intents);
        return intents[0];
    }

    private static List<CalculationParameterIntent> FindAll(
        CalculationParameterExtractionResult result,
        string key)
    {
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();

        for (int index = 0;
            index < result.Intents.Count;
            index++)
        {
            if (string.Equals(
                result.Intents[index].Key,
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                intents.Add(result.Intents[index]);
            }
        }

        return intents;
    }
}
