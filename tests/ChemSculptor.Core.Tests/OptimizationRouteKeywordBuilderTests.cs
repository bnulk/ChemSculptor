using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>优化 route 关键词构建测试。</summary>
public class OptimizationRouteKeywordBuilderTests
{
    /// <summary>验证优化 route 不包含 SP，且只生成一个 opt 任务关键词。</summary>
    [Fact]
    public void BuildsMergedOptimizationRoute()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationMaxCycles] = "200";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationInitialHessian] = "CalcFC";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.DoesNotContain("SP", result.RouteLine);
        Assert.Contains(
            "opt=(CalcFC, MaxCycles=200)",
            result.RouteLine);
        Assert.Equal(
            1,
            CountOccurrences(result.RouteLine, "opt="));
    }

    /// <summary>验证 Restart 会附带检查点几何和猜测读取。</summary>
    [Fact]
    public void RestartAddsCheckpointKeywords()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationRestart] = "true";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Contains("Restart", result.RouteLine);
        Assert.Contains("geom=check", result.RouteLine);
        Assert.Contains("guess=read", result.RouteLine);
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
