using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;

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
    private readonly GaussianRouteKeywordBuilder _routeKeywordBuilder;

    /// <summary>创建恢复输入写入器。</summary>
    public GaussianRecoveryInputWriter()
        : this(new GaussianRouteKeywordBuilder())
    {
    }

    /// <summary>创建恢复输入写入器。</summary>
    public GaussianRecoveryInputWriter(
        GaussianRouteKeywordBuilder routeKeywordBuilder)
    {
        if (routeKeywordBuilder == null)
        {
            throw new ArgumentNullException(
                nameof(routeKeywordBuilder));
        }

        _routeKeywordBuilder = routeKeywordBuilder;
    }

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

    /// <summary>
    /// 根据修正后的优化方案重建 Gaussian route，并保留分子坐标和标题。
    /// </summary>
    public async Task<GaussianRecoveryInputWriteResult>
        ApplyOptimizationCorrectionsAsync(
            string sourceInputPath,
            string targetInputPath,
            CalculationSpec recoverySpec,
            bool addStableOpt,
            CancellationToken cancellationToken = default)
    {
        GaussianRecoveryInputWriteResult result =
            new GaussianRecoveryInputWriteResult();

        if (string.IsNullOrWhiteSpace(sourceInputPath)
            || !File.Exists(sourceInputPath))
        {
            result.Succeeded = false;
            result.Error = "原始 Gaussian 输入文件不存在。";
            return result;
        }

        if (recoverySpec == null
            || (recoverySpec.TaskType
                    != CalculationTaskType.Optimization
                && recoverySpec.TaskType
                    != CalculationTaskType.OptimizationFrequency))
        {
            result.Succeeded = false;
            result.Error =
                "恢复方案不是几何优化或联合优化频率任务。";
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
        string routeLine = _routeKeywordBuilder.Build(
            recoverySpec).RouteLine;

        if (addStableOpt)
        {
            routeLine = routeLine + " stable=opt";
        }

        bool routeUpdated = false;
        bool chargeMultiplicityUpdated = false;

        for (int index = 0; index < lines.Length; index++)
        {
            string trimmed = lines[index].Trim();

            if (trimmed.StartsWith(
                "%chk=",
                StringComparison.OrdinalIgnoreCase))
            {
                lines[index] = "%chk=" + checkpointName;
                continue;
            }

            if (!routeUpdated
                && trimmed.StartsWith(
                    "#",
                    StringComparison.Ordinal))
            {
                lines[index] = routeLine;
                routeUpdated = true;
                continue;
            }

            int charge;
            int multiplicity;

            if (!chargeMultiplicityUpdated
                && TryParseChargeMultiplicity(
                    trimmed,
                    out charge,
                    out multiplicity))
            {
                lines[index] =
                    recoverySpec.Charge.ToString(
                        System.Globalization.CultureInfo.InvariantCulture) +
                    " " +
                    recoverySpec.Multiplicity.ToString(
                        System.Globalization.CultureInfo.InvariantCulture);
                chargeMultiplicityUpdated = true;
            }
        }

        if (!routeUpdated)
        {
            result.Succeeded = false;
            result.Error = "原始 Gaussian 输入中没有找到 route line。";
            return result;
        }

        if (!chargeMultiplicityUpdated)
        {
            result.Succeeded = false;
            result.Error =
                "原始 Gaussian 输入中没有找到电荷和自旋多重度行。";
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
