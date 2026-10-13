using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>规则版计算参数提取器测试。</summary>
public class RuleBasedCalculationParameterExtractorTests
{
    /// <summary>验证一条对话可以提取全部首期参数。</summary>
    [Fact]
    public async Task ExtractsAllFrozenParametersFromConversationText()
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult result =
            await extractor.ExtractAsync(
                "单点计算，CAM-B3LYP/6-31G*，加色散，nosymm，" +
                "SCF 最多 300，水溶剂，电荷 0，三重态",
                CreateContext());

        Assert.Equal(
            "CAM-B3LYP",
            FindSingle(result, CalculationOptionKeys.Method).Value);
        Assert.Equal(
            "6-31G*",
            FindSingle(result, CalculationOptionKeys.Basis).Value);
        Assert.Equal(
            CalculationParameterIntentState.KeyOnly,
            FindSingle(
                result,
                CalculationOptionKeys.DispersionCorrection).State);
        Assert.Equal(
            "Ignore",
            FindSingle(result, CalculationOptionKeys.Symmetry).Value);
        Assert.Equal(
            "300",
            FindSingle(
                result,
                CalculationOptionKeys.ScfIterationLimit).Value);
        Assert.Equal(
            "Water",
            FindSingle(result, CalculationOptionKeys.Solvent).Value);
        Assert.Equal(
            "0",
            FindSingle(result, CalculationOptionKeys.Charge).Value);
        Assert.Equal(
            "3",
            FindSingle(
                result,
                CalculationOptionKeys.Multiplicity).Value);
    }

    /// <summary>验证 GD3 保留为显式值。</summary>
    [Fact]
    public async Task ExtractsGd3AsExplicitValue()
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult result =
            await extractor.ExtractAsync(
                "单点计算，GD3",
                CreateContext());
        CalculationParameterIntent intent =
            FindSingle(
                result,
                CalculationOptionKeys.DispersionCorrection);

        Assert.Equal("GD3", intent.Value);
        Assert.Equal(
            CalculationParameterIntentState.ExplicitValue,
            intent.State);
        Assert.Equal("GD3", intent.RawText);
    }

    /// <summary>验证优化参数可以从对话和 Gaussian route 别名中提取。</summary>
    [Fact]
    public async Task ExtractsOptimizationParametersFromConversationText()
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult result =
            await extractor.ExtractAsync(
                "优化，opt=(maxcycles=300)，opt=tight，" +
                "opt=restart，CalcFC",
                CreateOptimizationContext());

        Assert.Equal(
            "300",
            FindSingle(
                result,
                CalculationOptionKeys.OptimizationMaxCycles).Value);
        Assert.Equal(
            "Tight",
            FindSingle(
                result,
                CalculationOptionKeys.OptimizationConvergence).Value);
        Assert.Equal(
            "true",
            FindSingle(
                result,
                CalculationOptionKeys.OptimizationRestart).Value);
        Assert.Equal(
            "CalcFC",
            FindSingle(
                result,
                CalculationOptionKeys.OptimizationInitialHessian).Value);
    }

    /// <summary>验证单点任务不会提取优化专用参数。</summary>
    [Fact]
    public async Task DoesNotExtractOptimizationParameterForSinglePoint()
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult result =
            await extractor.ExtractAsync(
                "opt=maxcycles=300",
                CreateContext());

        CalculationParameterIntent intent =
            Assert.Single(result.Intents);
        Assert.Equal(
            CalculationParameterIntentState.Unsupported,
            intent.State);
    }

    /// <summary>验证“跳过波函数稳定性检查”被提取为 Skip。</summary>
    [Fact]
    public async Task ExtractsWavefunctionStabilitySkip()
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult result =
            await extractor.ExtractAsync(
                "不做波函数稳定性检查",
                CreateContext());
        CalculationParameterIntent intent =
            FindSingle(
                result,
                CalculationOptionKeys.WavefunctionStabilityCheck);

        Assert.Equal("Skip", intent.Value);
    }

    /// <summary>验证同一条文本中的对称性冲突不会被自动合并。</summary>
    [Fact]
    public async Task PreservesConflictingSymmetryIntents()
    {
        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult result =
            await extractor.ExtractAsync(
                "nosymm 和 使用对称性",
                CreateContext());
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

    private static CalculationParameterExtractionContext
        CreateContext()
    {
        CalculationParameterExtractionContext context =
            new CalculationParameterExtractionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.SinglePoint;
        return context;
    }

    private static CalculationParameterExtractionContext
        CreateOptimizationContext()
    {
        CalculationParameterExtractionContext context =
            new CalculationParameterExtractionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.Optimization;
        return context;
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
