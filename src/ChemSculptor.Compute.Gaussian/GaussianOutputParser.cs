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

            Match match = ScfDonePattern.Match(line);
            if (!match.Success)
            {
                continue;
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
