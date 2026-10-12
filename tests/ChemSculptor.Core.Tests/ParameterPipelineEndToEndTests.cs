using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>对话参数到 Gaussian route 的端到端测试。</summary>
public class ParameterPipelineEndToEndTests
{
    /// <summary>验证冻结输入会生成期望的 Gaussian route 关键词。</summary>
    [Fact]
    public async Task ConversationParametersProduceGaussianRoute()
    {
        const string text =
            "单点计算，CAM-B3LYP/6-31G*，加 GD3BJ 色散校正，" +
            "nosymm，SCF 最多 300 次";

        CalculationParameterExtractionContext extractionContext =
            new CalculationParameterExtractionContext();
        extractionContext.Engine = CalculationDefaults.DefaultProgram;
        extractionContext.TaskType = CalculationTaskType.SinglePoint;

        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult extraction =
            await extractor.ExtractAsync(text, extractionContext);

        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        CalculationParameterResolutionContext resolutionContext =
            new CalculationParameterResolutionContext();
        resolutionContext.Engine =
            CalculationDefaults.DefaultProgram;
        resolutionContext.TaskType =
            CalculationTaskType.SinglePoint;
        resolutionContext.ElectronCount = 10;

        CalculationParameterResolutionResult resolution =
            new CalculationParameterResolver().Resolve(
                spec,
                extraction.Intents,
                resolutionContext);

        Assert.Equal(
            CalculationParameterResolutionStatus.Resolved,
            resolution.Status);

        GaussianRouteKeywordBuildResult route =
            new GaussianRouteKeywordBuilder().Build(resolution.Spec);

        Assert.StartsWith("#p", route.RouteLine);
        Assert.Contains("CAM-B3LYP/6-31G*", route.RouteLine);
        Assert.Contains("SP", route.RouteLine);
        Assert.Contains("em=GD3BJ", route.RouteLine);
        Assert.Contains("nosymm", route.RouteLine);
        Assert.Contains("scfcyc=300", route.RouteLine);
    }

    /// <summary>验证客户端直接输入 Gaussian route 行可以解析。</summary>
    [Fact]
    public async Task GaussianRouteLineProducesResolvedGaussianRoute()
    {
        const string text =
            "#p cam-b3lyp/6-31G* em=GD3BJ scfcyc=200 nosymm";

        CalculationParameterExtractionContext extractionContext =
            new CalculationParameterExtractionContext();
        extractionContext.Engine = CalculationDefaults.DefaultProgram;
        extractionContext.TaskType = CalculationTaskType.SinglePoint;

        RuleBasedCalculationParameterExtractor extractor =
            new RuleBasedCalculationParameterExtractor();
        CalculationParameterExtractionResult extraction =
            await extractor.ExtractAsync(text, extractionContext);

        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        CalculationParameterResolutionContext resolutionContext =
            new CalculationParameterResolutionContext();
        resolutionContext.Engine =
            CalculationDefaults.DefaultProgram;
        resolutionContext.TaskType =
            CalculationTaskType.SinglePoint;
        resolutionContext.ElectronCount = 10;

        CalculationParameterResolutionResult resolution =
            new CalculationParameterResolver().Resolve(
                spec,
                extraction.Intents,
                resolutionContext);

        Assert.Equal(
            CalculationParameterResolutionStatus.Resolved,
            resolution.Status);

        GaussianRouteKeywordBuildResult route =
            new GaussianRouteKeywordBuilder().Build(resolution.Spec);

        Assert.Contains("CAM-B3LYP/6-31G*", route.RouteLine);
        Assert.Contains("SP", route.RouteLine);
        Assert.Contains("em=GD3BJ", route.RouteLine);
        Assert.Contains("nosymm", route.RouteLine);
        Assert.Contains("scfcyc=200", route.RouteLine);
        Assert.Contains(
            resolution.AppliedParameters,
            parameter =>
                parameter.Key
                    == CalculationOptionKeys.RoutePrintLevel
                && parameter.Value == "Verbose");
    }
}
