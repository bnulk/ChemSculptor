using System.Globalization;
using System.Text.RegularExpressions;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.FrequencyAnalysis;

/// <summary>Gaussian 振动分析输出解析器。</summary>
public sealed class GaussianFrequencyOutputParser
{
    private static readonly Regex CorrectionPattern =
        new Regex(
            @"(?<name>Zero-point correction|Thermal correction to Energy|Thermal correction to Enthalpy|Thermal correction to Gibbs Free Energy)\s*=\s*(?<value>[-+]?[0-9]+(?:\.[0-9]+)?(?:[DdEe][+-]?[0-9]+)?)",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant);

    /// <summary>解析振动分析输出。</summary>
    public async Task<FrequencyAnalysisResult> ParseAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        FrequencyAnalysisResult result =
            new FrequencyAnalysisResult();
        result.OutputFilePath = outputPath;

        if (string.IsNullOrWhiteSpace(outputPath)
            || !File.Exists(outputPath))
        {
            return result;
        }

        string text = await File.ReadAllTextAsync(
            outputPath,
            cancellationToken);
        string[] lines = text.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                "Normal termination",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.NormalTermination = true;
            }

            if (line.IndexOf(
                "Frequencies --",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ParseFrequencyLine(line, result.Frequencies);
            }

            Match correction = CorrectionPattern.Match(line);

            if (correction.Success)
            {
                ApplyCorrection(
                    result,
                    correction.Groups["name"].Value,
                    correction.Groups["value"].Value);
            }
        }

        CalculateFrequencySummary(result);
        return result;
    }

    private static void ParseFrequencyLine(
        string line,
        List<double> frequencies)
    {
        int marker = line.IndexOf(
            "--",
            StringComparison.Ordinal);

        if (marker < 0)
        {
            return;
        }

        string valueText = line.Substring(marker + 2);
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] values = valueText.Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);

        for (int index = 0; index < values.Length; index++)
        {
            double frequency;

            if (TryParseGaussianNumber(
                values[index],
                out frequency))
            {
                frequencies.Add(frequency);
            }
        }
    }

    private static void ApplyCorrection(
        FrequencyAnalysisResult result,
        string name,
        string valueText)
    {
        double value;

        if (!TryParseGaussianNumber(valueText, out value))
        {
            return;
        }

        if (string.Equals(
            name,
            "Zero-point correction",
            StringComparison.OrdinalIgnoreCase))
        {
            result.ZeroPointCorrection = value;
        }
        else if (string.Equals(
            name,
            "Thermal correction to Energy",
            StringComparison.OrdinalIgnoreCase))
        {
            result.ThermalCorrectionToEnergy = value;
        }
        else if (string.Equals(
            name,
            "Thermal correction to Enthalpy",
            StringComparison.OrdinalIgnoreCase))
        {
            result.ThermalCorrectionToEnthalpy = value;
        }
        else if (string.Equals(
            name,
            "Thermal correction to Gibbs Free Energy",
            StringComparison.OrdinalIgnoreCase))
        {
            result.ThermalCorrectionToGibbs = value;
        }
    }

    private static void CalculateFrequencySummary(
        FrequencyAnalysisResult result)
    {
        if (result.Frequencies.Count == 0)
        {
            return;
        }

        double minimum = result.Frequencies[0];

        for (int index = 0;
            index < result.Frequencies.Count;
            index++)
        {
            double frequency = result.Frequencies[index];

            if (frequency < minimum)
            {
                minimum = frequency;
            }

            if (frequency < 0.0)
            {
                result.ImaginaryFrequencyCount++;
            }
        }

        result.MinimumFrequency = minimum;
        result.IsMinimum =
            result.ImaginaryFrequencyCount == 0;
    }

    private static bool TryParseGaussianNumber(
        string text,
        out double value)
    {
        string normalized = text.Replace('D', 'E')
            .Replace('d', 'e');
        return double.TryParse(
            normalized,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }
}
