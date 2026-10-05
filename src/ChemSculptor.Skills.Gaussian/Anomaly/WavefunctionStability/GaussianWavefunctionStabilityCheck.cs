using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;

namespace ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;

/// <summary>
/// Gaussian 波函数稳定性检查。
/// 当前阶段读取已经生成的稳定性检查输出，不负责创建辅助作业。
/// </summary>
public sealed class GaussianWavefunctionStabilityCheck
    : IAnomalyCheck
{
    private static readonly AnomalyCheckDescriptor DescriptorValue =
        CreateDescriptor();

    private readonly GaussianWavefunctionStabilityParser _parser;

    /// <summary>创建稳定性检查。</summary>
    public GaussianWavefunctionStabilityCheck(
        GaussianWavefunctionStabilityParser parser)
    {
        if (parser == null)
        {
            throw new ArgumentNullException(nameof(parser));
        }

        _parser = parser;
    }

    /// <summary>检查描述。</summary>
    public AnomalyCheckDescriptor Descriptor
    {
        get { return DescriptorValue; }
    }

    /// <summary>判断是否存在可检查的稳定性输出。</summary>
    public bool CanCheck(AnomalyContext context)
    {
        if (context == null)
        {
            return false;
        }

        string? outputPath;

        if (!context.Metadata.TryGetValue(
            AnomalyContextKeys.WavefunctionStabilityOutputPath,
            out outputPath))
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(outputPath)
            && File.Exists(outputPath);
    }

    /// <summary>解析稳定性输出并生成统一检查结果。</summary>
    public async Task<AnomalyCheckResult> CheckAsync(
        AnomalyContext context,
        CancellationToken cancellationToken = default)
    {
        AnomalyCheckResult checkResult = CreateBaseResult();
        checkResult.StartedAt = DateTimeOffset.UtcNow;

        if (!CanCheck(context))
        {
            checkResult.Status = AnomalyCheckStatus.Skipped;
            checkResult.SkippedReason =
                "没有可用的波函数稳定性检查输出。";
            checkResult.Summary = checkResult.SkippedReason;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        string outputPath =
            context.Metadata[AnomalyContextKeys.WavefunctionStabilityOutputPath];
        WavefunctionStabilityResult stabilityResult =
            await _parser.ParseAsync(outputPath, cancellationToken);

        checkResult.Evidence = new List<AnomalyEvidence>(
            stabilityResult.Evidence);
        checkResult.Summary = stabilityResult.Summary;

        if (stabilityResult.Status == WavefunctionStabilityStatus.Stable)
        {
            checkResult.Status = AnomalyCheckStatus.Passed;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        if (stabilityResult.Status == WavefunctionStabilityStatus.NotPerformed)
        {
            checkResult.Status = AnomalyCheckStatus.Skipped;
            checkResult.SkippedReason = stabilityResult.Summary;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        if (stabilityResult.Status == WavefunctionStabilityStatus.Inconclusive)
        {
            checkResult.Status = AnomalyCheckStatus.Inconclusive;
            checkResult.CompletedAt = DateTimeOffset.UtcNow;
            return checkResult;
        }

        AnomalyFinding finding = new AnomalyFinding();
        finding.Code = CommonAnomalyCodes.WavefunctionInstability;
        finding.Title = "波函数不稳定";
        finding.Category = AnomalyCategory.Scientific;
        finding.Severity = AnomalySeverity.Error;
        finding.Confidence = 1.0;
        finding.IsBlocking = true;
        finding.RequiresScientificJudgment = true;
        finding.Evidence = new List<AnomalyEvidence>(
            stabilityResult.Evidence);
        finding.Details["outputPath"] = outputPath;
        finding.Details["instabilityKind"] =
            stabilityResult.InstabilityKind;
        checkResult.Status = AnomalyCheckStatus.Finding;
        checkResult.Findings.Add(finding);
        checkResult.CompletedAt = DateTimeOffset.UtcNow;

        return checkResult;
    }

    private static AnomalyCheckResult CreateBaseResult()
    {
        AnomalyCheckResult result = new AnomalyCheckResult();
        result.Code = DescriptorValue.Code;
        result.DisplayName = DescriptorValue.DisplayName;
        result.Category = DescriptorValue.Category;
        result.Mechanism = DescriptorValue.Mechanism;
        result.IsRequired = DescriptorValue.IsRequired;
        return result;
    }

    private static AnomalyCheckDescriptor CreateDescriptor()
    {
        AnomalyCheckDescriptor descriptor =
            new AnomalyCheckDescriptor();
        descriptor.Code = CommonAnomalyCheckCodes.WavefunctionStability;
        descriptor.DisplayName = "波函数稳定性检查";
        descriptor.Category = AnomalyCategory.Scientific;
        descriptor.Mechanism =
            AnomalyCheckMechanism.AuxiliaryCalculation;
        descriptor.Version = "1.0.0";
        descriptor.IsRequired = true;
        descriptor.Description =
            "解析 Gaussian 波函数稳定性检查输出。";
        return descriptor;
    }
}
