using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian route 关键词翻译测试。</summary>
public class GaussianRouteKeywordBuilderTests
{
    /// <summary>验证冻结参数会翻译为对应 Gaussian 关键词。</summary>
    [Fact]
    public void BuildsFrozenRouteKeywords()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.DispersionCorrection] = "GD3BJ";
        spec.ExtraOptions[
            CalculationOptionKeys.Symmetry] = "Ignore";
        spec.ExtraOptions[
            CalculationOptionKeys.ScfIterationLimit] = "300";
        spec.Solvent = "Water";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Equal("CAM-B3LYP/6-31G*", result.MethodAndBasis);
        Assert.Equal("SP", result.TaskKeyword);
        Assert.Contains("em=GD3BJ", result.Keywords);
        Assert.Contains("nosymm", result.Keywords);
        Assert.Contains("scfcyc=300", result.Keywords);
        Assert.Contains(
            "SCRF=(Solvent=Water)",
            result.Keywords);
        Assert.Contains("CAM-B3LYP/6-31G*", result.RouteLine);
        Assert.Contains("SP", result.RouteLine);
        Assert.Contains("em=GD3BJ", result.RouteLine);
        Assert.Contains("nosymm", result.RouteLine);
        Assert.Contains("scfcyc=300", result.RouteLine);
        Assert.Contains("SCRF=(Solvent=Water)", result.RouteLine);
        Assert.Contains(
            result.Translations,
            translation =>
                translation.Key
                    == CalculationOptionKeys.DispersionCorrection
                && translation.RawKeyword == "em=GD3BJ");
        Assert.Contains(
            result.Translations,
            translation =>
                translation.Key == CalculationOptionKeys.Symmetry
                && translation.RawKeyword == "nosymm");
    }

    /// <summary>验证 GD3 和 None 的映射。</summary>
    [Fact]
    public void MapsGd3AndNoneDispersion()
    {
        CalculationSpec gd3Spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        gd3Spec.ExtraOptions[
            CalculationOptionKeys.DispersionCorrection] = "GD3";

        GaussianRouteKeywordBuildResult gd3Result =
            new GaussianRouteKeywordBuilder().Build(gd3Spec);

        Assert.Contains("em=GD3", gd3Result.Keywords);
        Assert.DoesNotContain("em=GD3BJ", gd3Result.Keywords);

        CalculationSpec noneSpec =
            CalculationDefaults.CreateDefaultSinglePoint();
        noneSpec.ExtraOptions[
            CalculationOptionKeys.DispersionCorrection] = "None";

        GaussianRouteKeywordBuildResult noneResult =
            new GaussianRouteKeywordBuilder().Build(noneSpec);

        Assert.DoesNotContain(
            noneResult.Keywords,
            keyword => keyword.StartsWith(
                "em=",
                StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>验证未知色散值会被拒绝。</summary>
    [Fact]
    public void RejectsUnknownDispersion()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.DispersionCorrection] = "Unknown";

        Assert.Throws<NotSupportedException>(
            () => new GaussianRouteKeywordBuilder().Build(spec));
    }

    /// <summary>验证 GD3BJ 输出 em=GD3BJ。</summary>
    [Fact]
    public void EmitsEmGd3bj()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.DispersionCorrection] = "GD3BJ";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Contains("em=GD3BJ", result.Keywords);
    }

    /// <summary>验证关闭对称性输出 nosymm。</summary>
    [Fact]
    public void EmitsNosymm()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.Symmetry] = "Ignore";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Contains("nosymm", result.Keywords);
    }

    /// <summary>验证 SCF 迭代上限输出 scfcyc。</summary>
    [Fact]
    public void EmitsScfcyc()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.ScfIterationLimit] = "300";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Contains("scfcyc=300", result.Keywords);
    }

    /// <summary>验证输出序列稳定，但不将顺序作为参数语义契约。</summary>
    [Fact]
    public void ProducesStableKeywordSequence()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.DispersionCorrection] = "GD3BJ";
        spec.ExtraOptions[
            CalculationOptionKeys.Symmetry] = "Ignore";
        spec.ExtraOptions[
            CalculationOptionKeys.ScfIterationLimit] = "300";

        GaussianRouteKeywordBuilder builder =
            new GaussianRouteKeywordBuilder();
        GaussianRouteKeywordBuildResult first =
            builder.Build(spec);
        GaussianRouteKeywordBuildResult second =
            builder.Build(spec);

        Assert.Equal(first.RouteLine, second.RouteLine);
    }

    /// <summary>验证优化任务不会输出 SP，并合并 opt 参数。</summary>
    [Fact]
    public void BuildsMergedOptimizationRoute()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationMaxCycles] = "300";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationConvergence] = "Tight";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationRestart] = "true";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationInitialHessian] = "CalcFC";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Equal(
            "opt=(CalcFC, Tight, Restart, MaxCycles=300)",
            result.TaskKeyword);
        Assert.DoesNotContain("SP", result.RouteLine);
        Assert.Contains("opt=(", result.RouteLine);
        Assert.Contains("geom=check", result.Keywords);
        Assert.Contains("guess=read", result.Keywords);
        Assert.Equal(
            1,
            CountOccurrences(result.RouteLine, "opt="));
        Assert.Contains(
            result.Translations,
            translation =>
                translation.Key
                    == CalculationOptionKeys.OptimizationMaxCycles
                && translation.RawKeyword == "MaxCycles=300");
        Assert.Contains(
            result.Translations,
            translation =>
                translation.Key
                    == CalculationOptionKeys.OptimizationRestart
                && translation.RawKeyword == "geom=check");
    }

    /// <summary>验证只有任务类型时会输出裸 opt。</summary>
    [Fact]
    public void BuildsBareOptimizationKeyword()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();
        spec.ExtraOptions.Clear();

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Equal("opt", result.TaskKeyword);
        Assert.DoesNotContain("SP", result.RouteLine);
    }

    /// <summary>验证优化关键词输出顺序稳定。</summary>
    [Fact]
    public void ProducesStableOptimizationKeywordSequence()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationMaxCycles] = "300";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationConvergence] = "VeryTight";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationInitialHessian] = "ReadFC";

        GaussianRouteKeywordBuilder builder =
            new GaussianRouteKeywordBuilder();
        GaussianRouteKeywordBuildResult first =
            builder.Build(spec);
        GaussianRouteKeywordBuildResult second =
            builder.Build(spec);

        Assert.Equal(first.RouteLine, second.RouteLine);
    }

    /// <summary>验证输出信息量会控制 route 前缀。</summary>
    [Fact]
    public void EmitsRoutePrintLevelPrefix()
    {
        AssertStartsWith(
            "Verbose",
            "#p");
        AssertStartsWith(
            "Normal",
            "#n");
        AssertStartsWith(
            "Terse",
            "#t");
        AssertStartsWith(
            "Default",
            "#");
    }

    private static void AssertStartsWith(
        string routePrintLevel,
        string prefix)
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        spec.ExtraOptions[
            CalculationOptionKeys.RoutePrintLevel] = routePrintLevel;

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.StartsWith(prefix, result.RouteLine);
        Assert.Contains(
            result.Translations,
            translation =>
                translation.Key
                    == CalculationOptionKeys.RoutePrintLevel
                && translation.RawKeyword == prefix);
    }

    private static int CountOccurrences(
        string text,
        string value)
    {
        int count = 0;
        int startIndex = 0;

        while (startIndex < text.Length)
        {
            int index = text.IndexOf(
                value,
                startIndex,
                StringComparison.OrdinalIgnoreCase);

            if (index < 0)
            {
                break;
            }

            count++;
            startIndex = index + value.Length;
        }

        return count;
    }
}
