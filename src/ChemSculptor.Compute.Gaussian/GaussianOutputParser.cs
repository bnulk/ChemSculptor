using System.Globalization;
using System.Text.RegularExpressions;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 输出文件解析器。
/// 只负责把文本解析为 Gaussian 专用数据结构，不生成通用计算结果。
/// </summary>
public sealed class GaussianOutputParser
{
    private static readonly Regex ScfDonePattern = new Regex(
        @"SCF Done:\s+E\(([^)]+)\)\s+=\s+(-?[0-9]+(?:\.[0-9]+)?(?:[DdEe][+-]?[0-9]+)?)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex ScfIterationsPattern = new Regex(
        @"after\s+([0-9]+)\s+cycles",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 解析 Gaussian 输出文件。
    /// 读取全部文本后逐行查找结束标志和最后一次 SCF Done。
    /// </summary>
    public async Task<GaussianOutput> ParseAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        GaussianOutput result = new GaussianOutput();
        result.OutputFilePath = outputPath;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return result;
        }

        if (!File.Exists(outputPath))
        {
            return result;
        }

        string outputText = await File.ReadAllTextAsync(outputPath, cancellationToken);
        string[] lines = outputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                "Normal termination of Gaussian",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.NormalTermination = true;
                continue;
            }

            if (line.IndexOf(
                "Error termination",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.ErrorTermination = true;
                result.ErrorMessages.Add(line.Trim());
                continue;
            }

            if (IsScfConvergenceFailure(line))
            {
                result.ScfConverged = false;
            }

            Match match = ScfDonePattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            result.ScfConverged = true;

            Match iterationsMatch =
                ScfIterationsPattern.Match(line);

            if (iterationsMatch.Success)
            {
                int iterations;

                if (int.TryParse(
                    iterationsMatch.Groups[1].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out iterations))
                {
                    result.ScfIterations = iterations;
                }
            }

            string energyText = match.Groups[2].Value;
            double energy;

            if (TryParseGaussianNumber(energyText, out energy))
            {
                result.Energy = energy;
                result.EnergyMethod = match.Groups[1].Value;
            }
        }

        return result;
    }

    /// <summary>判断一行是否表示 SCF 收敛失败。</summary>
    private static bool IsScfConvergenceFailure(string line)
    {
        if (line.IndexOf(
            "Convergence failure",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        if (line.IndexOf(
            "SCF has not converged",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return line.IndexOf(
            "No convergence in SCF",
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 解析 Gaussian 数值。
    /// Gaussian 有时使用 D 表示指数，例如 1.0D+02。
    /// </summary>
    private static bool TryParseGaussianNumber(string text, out double value)
    {
        string normalizedText = text.Replace('D', 'E').Replace('d', 'e');

        return double.TryParse(
            normalizedText,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

}
