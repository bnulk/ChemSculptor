using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.FrequencyAnalysis;

/// <summary>Gaussian 振动分析输入写入器。</summary>
public sealed class GaussianFrequencyInputWriter
{
    private readonly GaussianRouteKeywordBuilder _routeKeywordBuilder;

    /// <summary>创建振动分析输入写入器。</summary>
    public GaussianFrequencyInputWriter(
        GaussianRouteKeywordBuilder routeKeywordBuilder)
    {
        if (routeKeywordBuilder == null)
        {
            throw new ArgumentNullException(
                nameof(routeKeywordBuilder));
        }

        _routeKeywordBuilder = routeKeywordBuilder;
    }

    /// <summary>从最终优化输入重建 freq 输入。</summary>
    public async Task WriteAsync(
        CalculationSpec frequencySpec,
        string sourceInputPath,
        string targetInputPath,
        string checkpointFileName,
        CancellationToken cancellationToken = default)
    {
        if (frequencySpec == null
            || frequencySpec.TaskType
                != CalculationTaskType.Frequency)
        {
            throw new ArgumentException(
                "振动分析方案的任务类型必须为 Frequency。",
                nameof(frequencySpec));
        }

        if (string.IsNullOrWhiteSpace(sourceInputPath)
            || !File.Exists(sourceInputPath))
        {
            throw new FileNotFoundException(
                "原始 Gaussian 输入文件不存在。",
                sourceInputPath);
        }

        string inputText = await File.ReadAllTextAsync(
            sourceInputPath,
            cancellationToken);
        string[] lines = inputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);
        List<string> memoryLines = new List<string>();
        string title = "ChemSculptor frequency analysis";
        string chargeMultiplicity = string.Empty;
        bool routeFound = false;
        bool titleFound = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();

            if (trimmed.StartsWith(
                    "%mem=",
                    StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(
                    "%nprocshared=",
                    StringComparison.OrdinalIgnoreCase))
            {
                memoryLines.Add(trimmed);
                continue;
            }

            if (!routeFound
                && trimmed.StartsWith(
                    "#",
                    StringComparison.Ordinal))
            {
                routeFound = true;
                continue;
            }

            if (!routeFound || trimmed.Length == 0)
            {
                continue;
            }

            if (!titleFound)
            {
                title = trimmed;
                titleFound = true;
                continue;
            }

            if (IsChargeMultiplicityLine(trimmed))
            {
                chargeMultiplicity = trimmed;
                break;
            }
        }

        if (!routeFound
            || string.IsNullOrWhiteSpace(chargeMultiplicity))
        {
            throw new InvalidOperationException(
                "原始输入缺少 route 或电荷/多重度行。");
        }

        string routeLine =
            _routeKeywordBuilder.Build(frequencySpec).RouteLine +
            " guess=read geom=check";
        List<string> outputLines = new List<string>();
        outputLines.Add("%chk=" + checkpointFileName);

        for (int index = 0; index < memoryLines.Count; index++)
        {
            outputLines.Add(memoryLines[index]);
        }

        outputLines.Add(string.Empty);
        outputLines.Add(routeLine);
        outputLines.Add(string.Empty);
        outputLines.Add(title);
        outputLines.Add(string.Empty);
        outputLines.Add(chargeMultiplicity);
        outputLines.Add(string.Empty);

        string? directory = Path.GetDirectoryName(targetInputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            targetInputPath,
            string.Join(Environment.NewLine, outputLines) +
            Environment.NewLine,
            cancellationToken);
    }

    private static bool IsChargeMultiplicityLine(string line)
    {
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] parts = line.Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);
        int charge;
        int multiplicity;

        return parts.Length >= 2
            && int.TryParse(parts[0], out charge)
            && int.TryParse(parts[1], out multiplicity);
    }
}
