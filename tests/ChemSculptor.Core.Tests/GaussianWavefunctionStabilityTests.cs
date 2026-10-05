using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;

namespace ChemSculptor.Core.Tests;

/// <summary>Gaussian 波函数稳定性检查测试。</summary>
public class GaussianWavefunctionStabilityTests
{
    /// <summary>验证稳定输出被翻译为稳定状态。</summary>
    [Fact]
    public async Task ParsesStableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "stable.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is stable under the perturbations considered.");

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Stable,
                result.Status);
            Assert.Single(result.Evidence);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证不稳定输出被翻译为不稳定状态和类型。</summary>
    [Fact]
    public async Task ParsesUnstableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unstable.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Unstable,
                result.Status);
            Assert.Equal("internal", result.InstabilityKind);
            Assert.Single(result.Evidence);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证没有识别结论时返回可区分的状态。</summary>
    [Fact]
    public async Task ReturnsInconclusiveWhenStatementIsMissing()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unknown.log");

        try
        {
            await File.WriteAllTextAsync(path, "No stability result here.");

            GaussianWavefunctionStabilityParser parser =
                new GaussianWavefunctionStabilityParser();
            WavefunctionStabilityResult result =
                await parser.ParseAsync(path);

            Assert.Equal(
                WavefunctionStabilityStatus.Inconclusive,
                result.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证不稳定检查会产生科学异常发现。</summary>
    [Fact]
    public async Task DetectorCreatesScientificFinding()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unstable-detector.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            GaussianWavefunctionStabilityDetector detector =
                new GaussianWavefunctionStabilityDetector(
                    new GaussianWavefunctionStabilityParser());
            AnomalyContext context = new AnomalyContext();
            context.Metadata[
                AnomalyContextKeys.WavefunctionStabilityOutputPath] = path;

            IReadOnlyList<AnomalyFinding> findings =
                await detector.DetectAsync(context);

            Assert.Single(findings);
            Assert.Equal(
                CommonAnomalyCodes.WavefunctionInstability,
                findings[0].Code);
            Assert.Equal(
                AnomalyCategory.Scientific,
                findings[0].Category);
            Assert.True(findings[0].RequiresScientificJudgment);
            Assert.True(findings[0].IsBlocking);
            Assert.Equal(
                AnomalyCheckMechanism.AuxiliaryCalculation,
                detector.Descriptor.Mechanism);
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
