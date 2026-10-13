using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 几何优化输出解析测试。</summary>
public class GaussianOptimizationOutputParserTests
{
    /// <summary>验证每一步能量、SCF 状态、几何和最终收敛状态。</summary>
    [Fact]
    public async Task ParsesOptimizationTrajectoryAndConvergence()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "optimization.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                CreateOptimizationOutputText());

            GaussianOutputParser parser =
                new GaussianOutputParser();
            GaussianOutput gaussianOutput =
                await parser.ParseAsync(outputPath);
            GaussianResultTranslator translator =
                new GaussianResultTranslator();
            OptimizationResult result =
                translator.TranslateOptimization(
                    gaussianOutput);

            Assert.True(result.NormalTermination);
            Assert.True(result.OptimizationConverged);
            Assert.Equal(2, result.StepCount);
            Assert.Equal(2, result.Trajectory.Count);
            Assert.Equal(-76.200000, result.FinalEnergy);
            Assert.NotNull(result.FinalConvergence);
            Assert.True(result.FinalConvergence.Converged);
            Assert.True(
                result.FinalConvergence
                    .MaximumForceConverged);
            Assert.True(
                result.FinalConvergence.RmsForceConverged);
            Assert.True(
                result.FinalConvergence
                    .MaximumDisplacementConverged);
            Assert.True(
                result.FinalConvergence
                    .RmsDisplacementConverged);
            Assert.Equal(
                0.000450,
                result.FinalConvergence
                    .MaximumForceThreshold);
            Assert.Equal(
                0.000100,
                result.FinalConvergence.MaximumForce);
            Assert.Equal(
                0.000040,
                result.FinalConvergence.RmsForce);
            Assert.Equal(
                0.000300,
                result.FinalConvergence.MaximumDisplacement);
            Assert.Equal(
                0.000100,
                result.FinalConvergence.RmsDisplacement);

            OptimizationStep firstStep =
                result.Trajectory[0];
            Assert.Equal(1, firstStep.StepIndex);
            Assert.Equal(-76.100000, firstStep.Energy);
            Assert.True(firstStep.ScfConverged);
            Assert.Equal(7, firstStep.ScfIterations);
            Assert.NotNull(firstStep.Convergence);
            Assert.False(firstStep.Convergence.Converged);
            Assert.False(
                firstStep.Convergence
                    .MaximumForceConverged);
            Assert.Equal(2, firstStep.Geometry.Atoms.Count);

            OptimizationStep secondStep =
                result.Trajectory[1];
            Assert.Equal(2, secondStep.StepIndex);
            Assert.Equal(-76.200000, secondStep.Energy);
            Assert.True(secondStep.ScfConverged);
            Assert.Equal(5, secondStep.ScfIterations);
            Assert.Equal(2, secondStep.Geometry.Atoms.Count);
            Assert.Equal(
                "O",
                secondStep.Geometry.Atoms[0].Element);
            Assert.Equal(
                "H",
                secondStep.Geometry.Atoms[1].Element);
            Assert.Equal(
                0.000500,
                secondStep.Geometry.Atoms[0].X);
            Assert.Equal(2, result.FinalGeometry.Atoms.Count);
            Assert.Equal(
                0.000500,
                result.FinalGeometry.Atoms[0].X);
            Assert.Equal(
                OptimizationFailureKind.None,
                result.FailureKind);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证达到最大步骤数但没有完成优化时给出失败分类。</summary>
    [Fact]
    public async Task ReportsMaxCyclesExceeded()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "max-cycles.log");

        try
        {
            string outputText =
                " Step number 100 out of a maximum of 100\n" +
                CreateGeometryBlock(
                    "0.000000",
                    "0.000000",
                    "0.100000") +
                " SCF Done:  E(RB3LYP) =  -76.100000     A.U. after    9 cycles\n" +
                CreateConvergenceBlock("NO") +
                " Normal termination of Gaussian 16\n";

            await File.WriteAllTextAsync(outputPath, outputText);

            GaussianOutputParser parser =
                new GaussianOutputParser();
            GaussianOutput gaussianOutput =
                await parser.ParseAsync(outputPath);
            GaussianResultTranslator translator =
                new GaussianResultTranslator();
            OptimizationResult result =
                translator.TranslateOptimization(
                    gaussianOutput);

            Assert.False(result.OptimizationConverged);
            Assert.Equal(
                OptimizationFailureKind.MaxCyclesExceeded,
                result.FailureKind);
            Assert.Equal(100, result.StepCount);
            Assert.NotNull(result.FinalConvergence);
            Assert.False(result.FinalConvergence.Converged);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string CreateOptimizationOutputText()
    {
        return
            " Step number   1  out of a maximum of  100\n" +
            CreateGeometryBlock(
                "0.000000",
                "0.000000",
                "0.100000") +
            " SCF Done:  E(RB3LYP) =  -76.100000     A.U. after    7 cycles\n" +
            CreateConvergenceBlock("NO") +
            " Step number   2  out of a maximum of  100\n" +
            CreateGeometryBlock(
                "0.000500",
                "0.000000",
                "0.100000") +
            " SCF Done:  E(RB3LYP) =  -76.200000     A.U. after    5 cycles\n" +
            CreateConvergenceBlock("YES") +
            " Optimization completed.\n" +
            "    -- Stationary point found.\n" +
            " Normal termination of Gaussian 16\n";
    }

    private static string CreateGeometryBlock(
        string oxygenX,
        string oxygenY,
        string oxygenZ)
    {
        return
            "                         Standard orientation:\n" +
            " ---------------------------------------------------------------------\n" +
            " Center     Atomic      Atomic             Coordinates (Angstroms)\n" +
            " Number     Number       Type             X           Y           Z\n" +
            " ---------------------------------------------------------------------\n" +
            "      1          8           0        " +
            oxygenX +
            "        " +
            oxygenY +
            "        " +
            oxygenZ +
            "\n" +
            "      2          1           0        0.000000        0.700000       -0.400000\n" +
            " ---------------------------------------------------------------------\n";
    }

    private static string CreateConvergenceBlock(
        string converged)
    {
        return
            " Maximum Force            0.000100     0.000450     " +
            converged +
            "\n" +
            " RMS     Force            0.000040     0.000300     " +
            converged +
            "\n" +
            " Maximum Displacement     0.000300     0.001800     " +
            converged +
            "\n" +
            " RMS     Displacement     0.000100     0.001200     " +
            converged +
            "\n";
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
