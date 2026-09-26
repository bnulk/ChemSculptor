using System.Globalization;
using System.Text.RegularExpressions;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 输出文件解析器。
/// 当前阶段只提取正常结束标志、最终 SCF 能量和基本诊断信息。
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
    public async Task<CalculationResult> ParseAsync(
        string jobId,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        CalculationResult result = new CalculationResult();
        result.JobId = jobId;
        result.Program = Gaussian16ProgramAdapter.ProgramNameValue;
        result.OutputFilePath = outputPath;

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.output_path_missing",
                "Gaussian 输出文件路径为空。");
            return result;
        }

        if (!File.Exists(outputPath))
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.output_not_found",
                "没有找到 Gaussian 输出文件：" + outputPath);
            return result;
        }

        string outputText = await File.ReadAllTextAsync(outputPath, cancellationToken);
        string[] lines = outputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);

        bool normalTerminationFound = false;
        bool errorTerminationFound = false;
        bool energyFound = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                "Normal termination of Gaussian",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                normalTerminationFound = true;
                result.NormalTermination = true;
                continue;
            }

            if (line.IndexOf(
                "Error termination",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                errorTerminationFound = true;
                AddDiagnostic(
                    result,
                    CalculationDiagnosticSeverity.Error,
                    "gaussian.error_termination",
                    line.Trim());
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
                result.Method = match.Groups[1].Value;
                energyFound = true;
            }
            else
            {
                AddDiagnostic(
                    result,
                    CalculationDiagnosticSeverity.Warning,
                    "gaussian.energy_parse_failed",
                    "无法解析 SCF 能量：" + energyText);
            }
        }

        if (normalTerminationFound)
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.normal_termination",
                "Gaussian 正常结束。");
        }
        else if (!errorTerminationFound)
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.normal_termination_missing",
                "输出中没有找到 Gaussian 正常结束标志。");
        }

        if (energyFound)
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Info,
                "gaussian.energy_found",
                "已提取最终 SCF 能量。");
        }
        else
        {
            AddDiagnostic(
                result,
                CalculationDiagnosticSeverity.Error,
                "gaussian.energy_not_found",
                "输出中没有找到可解析的 SCF Done 能量。");
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

    private static void AddDiagnostic(
        CalculationResult result,
        CalculationDiagnosticSeverity severity,
        string code,
        string message)
    {
        CalculationDiagnostic diagnostic = new CalculationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }
}
