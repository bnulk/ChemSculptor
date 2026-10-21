using System.Globalization;
using System.Text.RegularExpressions;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.FrequencyAnalysis;

/// <summary>Gaussian 振动分析输出解析器。</summary>
public sealed class GaussianFrequencyOutputParser
{
    private static readonly Regex FrequencyLinePattern =
        new Regex(
            @"^\s*Frequencies\s*-{2,}\s*(?<values>.*)$",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant);

    private static readonly Regex OrientationCoordinatePattern =
        new Regex(
            @"^\s*[0-9]+\s+[0-9]+\s+[0-9]+\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s+([-+]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)(?:[DdEe][+-]?[0-9]+)?)\s*$",
            RegexOptions.Compiled
            | RegexOptions.CultureInvariant);

    private static readonly Regex AtomCountPattern =
        new Regex(
            @"NAtoms=\s*([0-9]+)",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant);

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
        List<double> fallbackFrequencies = new List<double>();
        List<double> currentHarmonicFrequencies =
            new List<double>();
        bool insideHarmonicBlock = false;

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
                "Harmonic frequencies",
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                currentHarmonicFrequencies =
                    new List<double>();
                insideHarmonicBlock = true;
            }

            Match frequencyMatch =
                FrequencyLinePattern.Match(line);

            if (frequencyMatch.Success)
            {
                List<double> target =
                    insideHarmonicBlock
                        ? currentHarmonicFrequencies
                        : fallbackFrequencies;
                ParseFrequencyValues(
                    frequencyMatch.Groups["values"].Value,
                    target);
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

        if (insideHarmonicBlock
            && currentHarmonicFrequencies.Count > 0)
        {
            result.Frequencies =
                currentHarmonicFrequencies;
        }
        else
        {
            result.Frequencies = fallbackFrequencies;
        }

        MoleculeShape shape =
            ReadMoleculeShape(lines);
        ApplyExpectedModeCount(result, shape);
        CalculateFrequencySummary(result);
        return result;
    }

    private static void ParseFrequencyValues(
        string valueText,
        List<double> frequencies)
    {
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

    private static MoleculeShape ReadMoleculeShape(
        string[] lines)
    {
        MoleculeShape shape = new MoleculeShape();
        List<AtomPosition> positions =
            new List<AtomPosition>();
        bool insideGeometry = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                "Input orientation:",
                StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf(
                    "Standard orientation:",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                insideGeometry = true;
                positions = new List<AtomPosition>();
                continue;
            }

            if (!insideGeometry)
            {
                continue;
            }

            Match coordinate =
                OrientationCoordinatePattern.Match(line);

            if (coordinate.Success)
            {
                AtomPosition position = new AtomPosition();
                double x;
                double y;
                double z;
                TryParseGaussianNumber(
                    coordinate.Groups[1].Value,
                    out x);
                TryParseGaussianNumber(
                    coordinate.Groups[2].Value,
                    out y);
                TryParseGaussianNumber(
                    coordinate.Groups[3].Value,
                    out z);
                position.X = x;
                position.Y = y;
                position.Z = z;
                positions.Add(position);
                continue;
            }

            if (positions.Count > 0
                && line.IndexOf(
                    "-----",
                    StringComparison.Ordinal) >= 0)
            {
                insideGeometry = false;
            }
        }

        if (positions.Count > 0)
        {
            shape.AtomCount = positions.Count;
            shape.IsLinear = IsLinearGeometry(positions);
            return shape;
        }

        Match atomCountMatch = null!;

        for (int index = lines.Length - 1;
            index >= 0;
            index--)
        {
            atomCountMatch =
                AtomCountPattern.Match(lines[index]);

            if (atomCountMatch.Success
                && int.TryParse(
                    atomCountMatch.Groups[1].Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int atomCount))
            {
                shape.AtomCount = atomCount;
                shape.IsLinear = atomCount == 2
                    || ContainsLinearPointGroup(lines);
                break;
            }
        }

        return shape;
    }

    private static bool IsLinearGeometry(
        List<AtomPosition> positions)
    {
        if (positions.Count == 2)
        {
            return true;
        }

        if (positions.Count < 2)
        {
            return false;
        }

        AtomPosition origin = positions[0];
        AtomPosition farthest = positions[1];
        double farthestDistance = 0.0;

        for (int index = 1;
            index < positions.Count;
            index++)
        {
            double distance =
                DistanceSquared(origin, positions[index]);

            if (distance > farthestDistance)
            {
                farthestDistance = distance;
                farthest = positions[index];
            }
        }

        double x = farthest.X - origin.X;
        double y = farthest.Y - origin.Y;
        double z = farthest.Z - origin.Z;
        double norm = Math.Sqrt(
            x * x + y * y + z * z);

        if (norm < 1.0E-10)
        {
            return false;
        }

        x = x / norm;
        y = y / norm;
        z = z / norm;
        double linearityTolerance = 1.0E-3;

        for (int index = 1;
            index < positions.Count;
            index++)
        {
            double dx = positions[index].X - origin.X;
            double dy = positions[index].Y - origin.Y;
            double dz = positions[index].Z - origin.Z;
            double crossX = dy * z - dz * y;
            double crossY = dz * x - dx * z;
            double crossZ = dx * y - dy * x;
            double perpendicularDistance =
                Math.Sqrt(
                    crossX * crossX
                    + crossY * crossY
                    + crossZ * crossZ);

            if (perpendicularDistance
                > linearityTolerance)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsLinearPointGroup(
        string[] lines)
    {
        for (int index = 0;
            index < lines.Length;
            index++)
        {
            string line = lines[index];

            if (line.IndexOf(
                    "Full point group",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (line.IndexOf(
                    "D*H",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf(
                    "C*V",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf(
                    "DINFH",
                    StringComparison.OrdinalIgnoreCase) >= 0
                || line.IndexOf(
                    "CINFV",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static double DistanceSquared(
        AtomPosition left,
        AtomPosition right)
    {
        double x = left.X - right.X;
        double y = left.Y - right.Y;
        double z = left.Z - right.Z;
        return x * x + y * y + z * z;
    }

    private static void ApplyExpectedModeCount(
        FrequencyAnalysisResult result,
        MoleculeShape shape)
    {
        result.AtomCount = shape.AtomCount;
        result.IsLinear = shape.IsLinear;

        if (!shape.AtomCount.HasValue
            || !shape.IsLinear.HasValue
            || shape.AtomCount.Value < 2)
        {
            return;
        }

        int atomCount = shape.AtomCount.Value;
        int expectedCount = shape.IsLinear.Value
            ? 3 * atomCount - 5
            : 3 * atomCount - 6;
        result.ExpectedFrequencyCount = expectedCount;
        result.FrequencyCountMatchesExpected =
            result.Frequencies.Count == expectedCount;

        if (!result.FrequencyCountMatchesExpected.Value)
        {
            CalculationDiagnostic diagnostic =
                new CalculationDiagnostic();
            diagnostic.Code =
                "gaussian.frequency.mode_count_mismatch";
            diagnostic.Severity =
                CalculationDiagnosticSeverity.Warning;
            diagnostic.Message =
                "振动模式数与 " +
                (shape.IsLinear.Value ? "3N-5" : "3N-6") +
                " 不一致：期望 " +
                expectedCount.ToString(
                    CultureInfo.InvariantCulture) +
                "，实际 " +
                result.Frequencies.Count.ToString(
                    CultureInfo.InvariantCulture) +
                "。";
            result.Diagnostics.Add(diagnostic);
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

    private sealed class MoleculeShape
    {
        public int? AtomCount { get; set; }

        public bool? IsLinear { get; set; }
    }

    private sealed class AtomPosition
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Z { get; set; }
    }
}
