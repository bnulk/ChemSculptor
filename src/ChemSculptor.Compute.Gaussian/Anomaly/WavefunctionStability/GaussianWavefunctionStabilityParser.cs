using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;

/// <summary>
/// Gaussian 波函数稳定性输出解析器。
/// 只把程序专用输出翻译为通用稳定性结果。
/// </summary>
public sealed class GaussianWavefunctionStabilityParser
{
    /// <summary>解析稳定性检查输出。</summary>
    public async Task<WavefunctionStabilityResult> ParseAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        WavefunctionStabilityResult result =
            new WavefunctionStabilityResult();
        result.OutputFilePath = outputPath;

        if (string.IsNullOrWhiteSpace(outputPath)
            || !File.Exists(outputPath))
        {
            result.Status = WavefunctionStabilityStatus.NotPerformed;
            result.Summary = "没有找到波函数稳定性检查输出。";
            return result;
        }

        string outputText = await File.ReadAllTextAsync(
            outputPath,
            cancellationToken);
        string[] lines = outputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);

        bool foundStableStatement = false;
        bool foundUnstableStatement = false;
        string instabilityKind = string.Empty;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();

            if (IsStableStatement(line))
            {
                foundStableStatement = true;
                AddEvidence(
                    result,
                    "gaussian.wavefunction_stability.stable",
                    line);
                continue;
            }

            if (IsUnstableStatement(line))
            {
                foundUnstableStatement = true;
                string currentKind = ExtractInstabilityKind(line);

                if (string.IsNullOrWhiteSpace(instabilityKind)
                    || string.Equals(
                        instabilityKind,
                        "unknown",
                        StringComparison.OrdinalIgnoreCase))
                {
                    instabilityKind = currentKind;
                }

                AddEvidence(
                    result,
                    "gaussian.wavefunction_stability.unstable",
                    line);
            }
        }

        if (foundUnstableStatement)
        {
            result.Status = WavefunctionStabilityStatus.Unstable;
            result.InstabilityKind = instabilityKind;
            result.Summary =
                "Gaussian 报告初始波函数不稳定；后续稳定性优化可能已找到稳定解。";
            return result;
        }

        if (foundStableStatement)
        {
            result.Status = WavefunctionStabilityStatus.Stable;
            result.Summary = "Gaussian 报告当前波函数稳定。";
            return result;
        }

        if (!foundStableStatement && !foundUnstableStatement)
        {
            result.Status = WavefunctionStabilityStatus.Inconclusive;
            result.Summary =
                "稳定性检查输出中没有找到可识别的稳定性结论。";
        }

        return result;
    }

    private static bool IsStableStatement(string line)
    {
        return line.IndexOf(
            "wavefunction is stable",
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsUnstableStatement(string line)
    {
        return line.IndexOf(
            "wavefunction is unstable",
            StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf(
                "wavefunction has",
                StringComparison.OrdinalIgnoreCase) >= 0
                && line.IndexOf(
                    "instability",
                    StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf(
                "internal instability",
                StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf(
                "external instability",
                StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ExtractInstabilityKind(string line)
    {
        if (line.IndexOf(
            "RHF -> UHF",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "RHF-to-UHF";
        }

        if (line.IndexOf(
            "internal",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "internal";
        }

        if (line.IndexOf(
            "external",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "external";
        }

        return "unknown";
    }

    private static void AddEvidence(
        WavefunctionStabilityResult result,
        string code,
        string message)
    {
        AnomalyEvidence evidence = new AnomalyEvidence();
        evidence.Code = code;
        evidence.Message = message;
        evidence.Source = "Gaussian stability output";
        result.Evidence.Add(evidence);
    }
}
