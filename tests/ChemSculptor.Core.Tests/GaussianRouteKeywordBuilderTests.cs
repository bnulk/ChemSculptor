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
}
