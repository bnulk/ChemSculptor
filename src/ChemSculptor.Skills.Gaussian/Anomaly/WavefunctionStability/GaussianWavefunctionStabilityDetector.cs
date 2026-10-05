using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;

namespace ChemSculptor.Skills.Gaussian.Anomaly.WavefunctionStability;

/// <summary>
/// Gaussian 波函数稳定性异常检测器。
/// 当前阶段读取已经生成的稳定性检查输出，不负责创建辅助作业。
/// </summary>
public sealed class GaussianWavefunctionStabilityDetector
    : IAnomalyDetector
{
    private static readonly AnomalyCheckDescriptor DescriptorValue =
        CreateDescriptor();

    private readonly GaussianWavefunctionStabilityParser _parser;

    /// <summary>创建检测器。</summary>
    public GaussianWavefunctionStabilityDetector(
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
    public bool CanDetect(AnomalyContext context)
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

    /// <summary>解析稳定性输出并生成异常发现。</summary>
    public async Task<IReadOnlyList<AnomalyFinding>> DetectAsync(
        AnomalyContext context,
        CancellationToken cancellationToken = default)
    {
        List<AnomalyFinding> findings = new List<AnomalyFinding>();

        if (!CanDetect(context))
        {
            return findings;
        }

        string outputPath =
            context.Metadata[AnomalyContextKeys.WavefunctionStabilityOutputPath];
        WavefunctionStabilityResult stabilityResult =
            await _parser.ParseAsync(outputPath, cancellationToken);

        if (stabilityResult.Status
            != WavefunctionStabilityStatus.Unstable)
        {
            return findings;
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
        findings.Add(finding);

        return findings;
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
