using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.FrequencyAnalysis;

namespace ChemSculptor.Core.Tests;

/// <summary>振动分析输入和输出测试。</summary>
public class FrequencyAnalysisTests
{
    /// <summary>验证 Gaussian 频率和热校正可以被解析。</summary>
    [Fact]
    public async Task ParsesFrequenciesAndThermalCorrections()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "frequency.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " Frequencies --    -10.0000    20.0000    30.0000\n" +
                " Zero-point correction=                           0.010000\n" +
                " Thermal correction to Energy=                    0.020000\n" +
                " Thermal correction to Enthalpy=                  0.030000\n" +
                " Thermal correction to Gibbs Free Energy=         0.040000\n" +
                " Normal termination of Gaussian 16\n");

            FrequencyAnalysisResult result =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.True(result.NormalTermination);
            Assert.Equal(3, result.Frequencies.Count);
            Assert.Equal(1, result.ImaginaryFrequencyCount);
            Assert.False(result.IsMinimum);
            Assert.Equal(-10.0, result.MinimumFrequency);
            Assert.Equal(0.01, result.ZeroPointCorrection);
            Assert.Equal(0.02, result.ThermalCorrectionToEnergy);
            Assert.Equal(0.03, result.ThermalCorrectionToEnthalpy);
            Assert.Equal(0.04, result.ThermalCorrectionToGibbs);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证 opt 输入会转换为 freq + guess/geom 输入。</summary>
    [Fact]
    public async Task WritesFrequencyInputFromOptimizationInput()
    {
        string root = CreateTemporaryRoot();
        string sourcePath = Path.Combine(root, "source.gjf");
        string targetPath = Path.Combine(root, "frequency.gjf");

        try
        {
            await File.WriteAllTextAsync(
                sourcePath,
                "%chk=source.chk\n" +
                "%mem=4GB\n" +
                "%nprocshared=4\n\n" +
                "#p CAM-B3LYP/6-31G* opt em=GD3BJ nosymm\n\n" +
                "frequency input test\n\n" +
                "0 1\n" +
                "O 0.0 0.0 0.0\n\n");

            CalculationSpec spec =
                CalculationDefaults.CreateDefaultOptimization();
            spec.TaskType = CalculationTaskType.Frequency;
            spec.ExtraOptions[
                CalculationOptionKeys.Symmetry] = "Ignore";

            await new GaussianFrequencyInputWriter(
                new GaussianRouteKeywordBuilder())
                .WriteAsync(
                    spec,
                    sourcePath,
                    targetPath,
                    "frequency.chk");
            string input = await File.ReadAllTextAsync(targetPath);

            Assert.Contains("Freq", input);
            Assert.DoesNotContain("OPT", input);
            Assert.Contains("guess=read", input);
            Assert.Contains("geom=check", input);
            Assert.Contains("em=GD3BJ", input);
            Assert.Contains("nosymm", input);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
