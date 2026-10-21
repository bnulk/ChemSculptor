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

    /// <summary>验证双原子线型分子使用 3N-5 个振动模式。</summary>
    [Fact]
    public async Task ParsesDiatomicLinearModeCount()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "o2-frequency.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " NAtoms=      2 NQM=        2\n" +
                " Full point group                 D*H     NOp   8\n" +
                " Low frequencies ---   -0.0006   -0.0003    8.4037\n" +
                " Harmonic frequencies (cm**-1)\n" +
                " Frequencies --   1727.4137\n" +
                " Normal termination of Gaussian 16\n");

            FrequencyAnalysisResult result =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.Equal(2, result.AtomCount);
            Assert.True(result.IsLinear);
            Assert.Equal(1, result.ExpectedFrequencyCount);
            Assert.True(result.FrequencyCountMatchesExpected);
            Assert.Single(result.Frequencies);
            Assert.Equal(1727.4137, result.Frequencies[0]);
            Assert.Empty(result.Diagnostics);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证三原子线型分子使用 3N-5 并累积分行频率。</summary>
    [Fact]
    public async Task ParsesLinearTriatomicFrequencies()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(
            root,
            "co2-frequency.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                CreateOrientationBlock(
                    "O",
                    "-1.160000",
                    "0.000000",
                    "0.000000",
                    "C",
                    "0.000000",
                    "0.000000",
                    "0.000000",
                    "O",
                    "1.160000",
                    "0.000000",
                    "0.000000") +
                " Harmonic frequencies (cm**-1)\n" +
                " Frequencies --   600.0000   600.0000   1300.0000\n" +
                " Frequencies --  2400.0000\n" +
                " Normal termination of Gaussian 16\n");

            FrequencyAnalysisResult result =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.Equal(3, result.AtomCount);
            Assert.True(result.IsLinear);
            Assert.Equal(4, result.ExpectedFrequencyCount);
            Assert.True(result.FrequencyCountMatchesExpected);
            Assert.Equal(4, result.Frequencies.Count);
            Assert.Empty(result.Diagnostics);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证非线性分子使用 3N-6 个振动模式。</summary>
    [Fact]
    public async Task ParsesNonlinearTriatomicFrequencies()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(
            root,
            "water-frequency.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                CreateOrientationBlock(
                    "O",
                    "0.000000",
                    "0.000000",
                    "0.117300",
                    "H",
                    "0.000000",
                    "0.757200",
                    "-0.469200",
                    "H",
                    "0.000000",
                    "-0.757200",
                    "-0.469200") +
                " Harmonic frequencies (cm**-1)\n" +
                " Frequencies --   1600.0000   3700.0000   3800.0000\n" +
                " Normal termination of Gaussian 16\n");

            FrequencyAnalysisResult result =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.Equal(3, result.AtomCount);
            Assert.False(result.IsLinear);
            Assert.Equal(3, result.ExpectedFrequencyCount);
            Assert.True(result.FrequencyCountMatchesExpected);
            Assert.Equal(3, result.Frequencies.Count);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证多次谐波块只采用最后一块。</summary>
    [Fact]
    public async Task UsesOnlyLastHarmonicFrequencyBlock()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(
            root,
            "multiple-blocks.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " NAtoms=      2\n" +
                " Full point group                 D*H\n" +
                " Harmonic frequencies (cm**-1)\n" +
                " Frequencies --   100.0000   200.0000   300.0000\n" +
                " Harmonic frequencies (cm**-1)\n" +
                " Frequencies --   1727.4137\n" +
                " Normal termination of Gaussian 16\n");

            FrequencyAnalysisResult result =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.Single(result.Frequencies);
            Assert.Equal(1727.4137, result.Frequencies[0]);
            Assert.True(result.FrequencyCountMatchesExpected);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证线型分子模式数不一致时产生诊断。</summary>
    [Fact]
    public async Task ReportsLinearModeCountMismatch()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(
            root,
            "mode-mismatch.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " NAtoms=      2\n" +
                " Full point group                 D*H\n" +
                " Harmonic frequencies (cm**-1)\n" +
                " Frequencies --   100.0000   200.0000\n" +
                " Normal termination of Gaussian 16\n");

            FrequencyAnalysisResult result =
                await new GaussianFrequencyOutputParser()
                    .ParseAsync(outputPath);

            Assert.Equal(1, result.ExpectedFrequencyCount);
            Assert.False(result.FrequencyCountMatchesExpected);
            Assert.Contains(
                result.Diagnostics,
                diagnostic =>
                    diagnostic.Code
                    == "gaussian.frequency.mode_count_mismatch");
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string CreateOrientationBlock(
        params string[] values)
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();
        builder.AppendLine(" Standard orientation:");
        builder.AppendLine(
            " ---------------------------------------------------------------------");
        builder.AppendLine(
            " Center     Atomic      Atomic             Coordinates (Angstroms)");
        builder.AppendLine(
            " Number     Number       Type             X           Y           Z");
        builder.AppendLine(
            " ---------------------------------------------------------------------");

        for (int index = 0;
            index < values.Length;
            index += 4)
        {
            builder.Append(" ");
            builder.Append((index / 4 + 1).ToString());
            builder.Append("          ");
            builder.Append("1");
            builder.Append("           0        ");
            builder.Append(values[index + 1]);
            builder.Append("        ");
            builder.Append(values[index + 2]);
            builder.Append("        ");
            builder.Append(values[index + 3]);
            builder.AppendLine();
        }

        builder.AppendLine(
            " ---------------------------------------------------------------------");
        return builder.ToString();
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
