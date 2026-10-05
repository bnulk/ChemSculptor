namespace ChemSculptor.Compute.Gaussian.Anomaly.WavefunctionStability;

/// <summary>Gaussian 稳定性检查输入文件写入结果。</summary>
public sealed class GaussianWavefunctionStabilityInputWriteResult
{
    /// <summary>输入文件是否准备成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>当前模型是否支持稳定性检查。</summary>
    public bool IsSupported { get; set; } = true;

    /// <summary>失败或跳过的原因。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Gaussian 稳定性检查输入文件写入器。
/// 保留原始方法和基组，并使用 guess=read geom=check stable。
/// </summary>
public sealed class GaussianWavefunctionStabilityInputWriter
{
    /// <summary>创建稳定性检查输入文件。</summary>
    public async Task<GaussianWavefunctionStabilityInputWriteResult> WriteAsync(
        string sourceInputPath,
        string targetInputPath,
        string checkpointFileName,
        CancellationToken cancellationToken = default)
    {
        GaussianWavefunctionStabilityInputWriteResult result =
            new GaussianWavefunctionStabilityInputWriteResult();

        if (string.IsNullOrWhiteSpace(sourceInputPath)
            || !File.Exists(sourceInputPath))
        {
            result.Succeeded = false;
            result.Message = "原始 Gaussian 输入文件不存在。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(checkpointFileName))
        {
            result.Succeeded = false;
            result.Message = "稳定性检查检查点文件名不能为空。";
            return result;
        }

        string inputText = await File.ReadAllTextAsync(
            sourceInputPath,
            cancellationToken);
        string[] lines = inputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);

        GaussianStabilityInputParts parts =
            ParseInputParts(lines);

        if (!parts.RouteFound)
        {
            result.Succeeded = false;
            result.Message = "没有找到 Gaussian 计算路线行。";
            return result;
        }

        if (parts.ChargeMultiplicity.Length == 0)
        {
            result.Succeeded = false;
            result.Message = "没有找到电荷和自旋多重度行。";
            return result;
        }

        if (parts.RouteLine.IndexOf(
            "oniom",
            StringComparison.OrdinalIgnoreCase) >= 0)
        {
            result.Succeeded = false;
            result.IsSupported = false;
            result.Message = "当前 ONIOM 输入不支持波函数稳定性检查。";
            return result;
        }

        string routeLine = BuildStabilityRoute(parts.RouteLine);
        List<string> outputLines = new List<string>();
        outputLines.Add("%chk=" + checkpointFileName);

        for (int index = 0; index < parts.MemoryLines.Count; index++)
        {
            outputLines.Add(parts.MemoryLines[index]);
        }

        outputLines.Add(string.Empty);
        outputLines.Add(routeLine);
        outputLines.Add(string.Empty);
        outputLines.Add(parts.Title);
        outputLines.Add(string.Empty);
        outputLines.Add(parts.ChargeMultiplicity);
        outputLines.Add(string.Empty);

        string? directory = Path.GetDirectoryName(targetInputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string outputText =
            string.Join(Environment.NewLine, outputLines) +
            Environment.NewLine;
        await File.WriteAllTextAsync(
            targetInputPath,
            outputText,
            cancellationToken);

        result.Succeeded = true;
        result.IsSupported = true;
        return result;
    }

    private static GaussianStabilityInputParts ParseInputParts(
        string[] lines)
    {
        GaussianStabilityInputParts parts =
            new GaussianStabilityInputParts();
        bool titleFound = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            string trimmed = line.Trim();

            if (trimmed.StartsWith(
                "%mem=",
                StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(
                    "%nprocshared=",
                    StringComparison.OrdinalIgnoreCase))
            {
                parts.MemoryLines.Add(trimmed);
                continue;
            }

            if (!parts.RouteFound
                && trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                parts.RouteLine = trimmed;
                parts.RouteFound = true;
                continue;
            }

            if (!parts.RouteFound || trimmed.Length == 0)
            {
                continue;
            }

            if (!titleFound)
            {
                parts.Title = trimmed;
                titleFound = true;
                continue;
            }

            if (IsChargeMultiplicityLine(trimmed))
            {
                parts.ChargeMultiplicity = trimmed;
                break;
            }
        }

        if (parts.Title.Length == 0)
        {
            parts.Title = "ChemSculptor wavefunction stability check";
        }

        return parts;
    }

    private static bool IsChargeMultiplicityLine(string line)
    {
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] parts = line.Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            return false;
        }

        int charge;
        int multiplicity;
        return int.TryParse(parts[0], out charge)
            && int.TryParse(parts[1], out multiplicity);
    }

    private static string BuildStabilityRoute(string routeLine)
    {
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] parts = routeLine.Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);
        List<string> keptParts = new List<string>();

        for (int index = 0; index < parts.Length; index++)
        {
            string part = parts[index];

            if (string.Equals(
                part,
                "SP",
                StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    part,
                    "OPT",
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(
                    part,
                    "FREQ",
                    StringComparison.OrdinalIgnoreCase)
                || part.StartsWith(
                    "stable",
                    StringComparison.OrdinalIgnoreCase)
                || part.StartsWith(
                    "guess=",
                    StringComparison.OrdinalIgnoreCase)
                || part.StartsWith(
                    "geom=",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            keptParts.Add(part);
        }

        keptParts.Add("guess=read");
        keptParts.Add("geom=check");
        keptParts.Add("stable");

        return string.Join(" ", keptParts);
    }

    private sealed class GaussianStabilityInputParts
    {
        public string RouteLine { get; set; } = string.Empty;

        public bool RouteFound { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ChargeMultiplicity { get; set; } = string.Empty;

        public List<string> MemoryLines { get; set; } =
            new List<string>();
    }
}
