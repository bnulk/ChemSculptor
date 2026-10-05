namespace ChemSculptor.Compute.Gaussian.Anomaly.Recovery;

/// <summary>Gaussian 派生恢复输入写入结果。</summary>
public sealed class GaussianRecoveryInputWriteResult
{
    /// <summary>是否写入成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>Gaussian 派生恢复输入写入器。</summary>
public sealed class GaussianRecoveryInputWriter
{
    /// <summary>修改 Gaussian 输入中的自旋多重度。</summary>
    public async Task<GaussianRecoveryInputWriteResult> ChangeMultiplicityAsync(
        string sourceInputPath,
        string targetInputPath,
        int targetMultiplicity,
        CancellationToken cancellationToken = default)
    {
        GaussianRecoveryInputWriteResult result =
            new GaussianRecoveryInputWriteResult();

        if (targetMultiplicity <= 0)
        {
            result.Succeeded = false;
            result.Error = "目标自旋多重度必须大于 0。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(sourceInputPath)
            || !File.Exists(sourceInputPath))
        {
            result.Succeeded = false;
            result.Error = "原始 Gaussian 输入文件不存在。";
            return result;
        }

        string inputText = await File.ReadAllTextAsync(
            sourceInputPath,
            cancellationToken);
        string[] lines = inputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);
        string checkpointName =
            Path.GetFileNameWithoutExtension(targetInputPath) + ".chk";
        bool multiplicityUpdated = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index];
            string trimmed = line.Trim();

            if (trimmed.StartsWith(
                "%chk=",
                StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = "%chk=" + checkpointName;
                continue;
            }

            int charge;
            int multiplicity;

            if (!multiplicityUpdated
                && TryParseChargeMultiplicity(
                    trimmed,
                    out charge,
                    out multiplicity))
            {
                lines[index] =
                    charge.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " " +
                    targetMultiplicity.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                multiplicityUpdated = true;
            }
        }

        if (!multiplicityUpdated)
        {
            result.Succeeded = false;
            result.Error = "没有找到 Gaussian 电荷和自旋多重度行。";
            return result;
        }

        string? directory = Path.GetDirectoryName(targetInputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string outputText =
            string.Join(Environment.NewLine, lines) +
            Environment.NewLine;
        await File.WriteAllTextAsync(
            targetInputPath,
            outputText,
            cancellationToken);

        result.Succeeded = true;
        return result;
    }

    private static bool TryParseChargeMultiplicity(
        string line,
        out int charge,
        out int multiplicity)
    {
        char[] separators = new char[1];
        separators[0] = ' ';
        string[] parts = line.Split(
            separators,
            StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2
            || !int.TryParse(parts[0], out charge)
            || !int.TryParse(parts[1], out multiplicity))
        {
            charge = 0;
            multiplicity = 0;
            return false;
        }

        return true;
    }
}
