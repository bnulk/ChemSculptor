using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;

namespace ChemSculptor.Core.Tests;

/// <summary>异常提供器注册表测试。</summary>
public class AnomalyProviderRegistryTests
{
    /// <summary>验证检测器可以注册并列出。</summary>
    [Fact]
    public void RegistersAndListsDetector()
    {
        AnomalyProviderRegistry registry =
            new AnomalyProviderRegistry();
        TestAnomalyDetector detector = new TestAnomalyDetector();

        registry.RegisterDetector(detector);

        IReadOnlyList<IAnomalyDetector> detectors =
            registry.ListDetectors();

        Assert.Single(detectors);
        Assert.Equal("test-anomaly", detectors[0].Code);
    }

    /// <summary>验证重复注册不会被静默覆盖。</summary>
    [Fact]
    public void RejectsDuplicateDetectorCode()
    {
        AnomalyProviderRegistry registry =
            new AnomalyProviderRegistry();
        registry.RegisterDetector(new TestAnomalyDetector());

        bool exceptionThrown = false;

        try
        {
            registry.RegisterDetector(new TestAnomalyDetector());
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        Assert.True(exceptionThrown);
    }

    private sealed class TestAnomalyDetector : IAnomalyDetector
    {
        public string Code
        {
            get { return "test-anomaly"; }
        }

        public string Version
        {
            get { return "1.0.0"; }
        }

        public bool CanDetect(AnomalyContext context)
        {
            return true;
        }

        public Task<IReadOnlyList<AnomalyFinding>> DetectAsync(
            AnomalyContext context,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AnomalyFinding> findings =
                new List<AnomalyFinding>();
            return Task.FromResult(findings);
        }
    }
}
