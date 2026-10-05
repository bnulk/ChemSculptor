using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Gaussian;
using ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.AnomalyWorkflow;

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

    /// <summary>验证不稳定检查会返回 Finding 和科学异常。</summary>
    [Fact]
    public async Task CheckReturnsFindingForUnstableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "unstable-detector.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            GaussianWavefunctionStabilityCheckSkill check =
                new GaussianWavefunctionStabilityCheckSkill(
                    new GaussianWavefunctionStabilityParser());
            AnomalyContext context = CreateGaussianContext(path);

            AnomalyCheckResult checkResult =
                await check.CheckAsync(context);

            Assert.Equal(
                AnomalyCheckStatus.Finding,
                checkResult.Status);
            Assert.Single(checkResult.Findings);
            Assert.Equal(
                CommonAnomalyCodes.WavefunctionInstability,
                checkResult.Findings[0].Code);
            Assert.Equal(
                AnomalyCategory.Scientific,
                checkResult.Findings[0].Category);
            Assert.True(
                checkResult.Findings[0].RequiresScientificJudgment);
            Assert.True(checkResult.Findings[0].IsBlocking);
            Assert.Equal(
                AnomalyCheckMechanism.AuxiliaryCalculation,
                check.Descriptor.Mechanism);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证稳定输出返回 Passed。</summary>
    [Fact]
    public async Task CheckReturnsPassedForStableWavefunction()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "stable-check.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is stable under the perturbations considered.");

            GaussianWavefunctionStabilityCheckSkill check =
                new GaussianWavefunctionStabilityCheckSkill(
                    new GaussianWavefunctionStabilityParser());
            AnomalyContext context = CreateGaussianContext(path);

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(AnomalyCheckStatus.Passed, result.Status);
            Assert.Empty(result.Findings);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证缺少输出时返回 Skipped 和跳过原因。</summary>
    [Fact]
    public async Task CheckReturnsSkippedWithoutOutput()
    {
        GaussianWavefunctionStabilityCheckSkill check =
            new GaussianWavefunctionStabilityCheckSkill(
                new GaussianWavefunctionStabilityParser());
        AnomalyContext context = CreateGaussianContext(string.Empty);

        AnomalyCheckResult result =
            await check.CheckAsync(context);

        Assert.Equal(AnomalyCheckStatus.Skipped, result.Status);
        Assert.Contains("没有可用", result.SkippedReason);
    }

    /// <summary>验证无法判断时返回 Inconclusive。</summary>
    [Fact]
    public async Task CheckReturnsInconclusiveWithoutStatement()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "inconclusive-check.log");

        try
        {
            await File.WriteAllTextAsync(path, "No stability conclusion.");

            GaussianWavefunctionStabilityCheckSkill check =
                new GaussianWavefunctionStabilityCheckSkill(
                    new GaussianWavefunctionStabilityParser());
            AnomalyContext context = CreateGaussianContext(path);

            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(
                AnomalyCheckStatus.Inconclusive,
                result.Status);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证通用 Skill 选择 Gaussian 专用 Skill。</summary>
    [Fact]
    public async Task GenericSkillDispatchesToGaussianCheck()
    {
        string root = CreateTemporaryRoot();
        string path = Path.Combine(root, "generic-dispatch.log");

        try
        {
            await File.WriteAllTextAsync(
                path,
                "The wavefunction is unstable with respect to internal perturbations.");

            AnomalyProviderRegistry registry =
                new AnomalyProviderRegistry();
            GaussianWavefunctionStabilityCheckSkill gaussianCheck =
                new GaussianWavefunctionStabilityCheckSkill(
                    new GaussianWavefunctionStabilityParser());
            registry.RegisterCheck(gaussianCheck);

            WavefunctionStabilityCheckSkill genericSkill =
                new WavefunctionStabilityCheckSkill(registry);
            AnomalyCheckRequest request = new AnomalyCheckRequest();
            request.CheckCode =
                CommonAnomalyCheckCodes.WavefunctionStability;
            request.Context = CreateGaussianContext(path);

            TaskRequest taskRequest = new TaskRequest();
            taskRequest.WorkflowId = "workflow-generic";
            taskRequest.NodeId = "stability-check";
            taskRequest.Inputs[
                JsonSkill<AnomalyCheckRequest, AnomalyCheckResult>.RequestKey] =
                SkillJson.Serialize(request);

            TaskResult taskResult =
                await genericSkill.ExecuteAsync(taskRequest);

            if (string.IsNullOrWhiteSpace(taskResult.Output))
            {
                throw new InvalidOperationException(
                    "通用 Skill 没有返回检查结果。");
            }

            AnomalyCheckResult checkResult =
                SkillJson.Deserialize<AnomalyCheckResult>(
                    taskResult.Output);

            Assert.True(taskResult.Succeeded);
            Assert.Equal(
                AnomalyCheckStatus.Finding,
                checkResult.Status);
            Assert.Equal(
                GaussianSkillIds.WavefunctionStabilityCheck,
                checkResult.ImplementationId);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static AnomalyContext CreateGaussianContext(string outputPath)
    {
        CalculationJob job = new CalculationJob();
        job.JobId = "job-stability";
        job.Spec = CalculationDefaults.CreateDefaultSinglePoint();

        AnomalyContext context = new AnomalyContext();
        context.Job = job;

        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            context.Metadata[
                AnomalyContextKeys.WavefunctionStabilityOutputPath] =
                outputPath;
        }

        return context;
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
