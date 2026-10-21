using System.Text.RegularExpressions;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.Nto;

/// <summary>Gaussian NTO 输入写入器。</summary>
public sealed class GaussianNtoInputWriter
{
    private static readonly Regex TdKeywordPattern =
        new Regex(
            @"(?i)(?<!\S)TD(?:\s*=\s*(?:\([^)]*\)|[^\s]+)|\([^)]*\))?(?=\s|$)",
            RegexOptions.Compiled
            | RegexOptions.CultureInvariant);

    /// <summary>从源 TDDFT 输入生成 NTO 输入。</summary>
    public async Task WriteAsync(
        NtoInputWriteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
                request.SourceInputPath)
            || !File.Exists(request.SourceInputPath))
        {
            throw new FileNotFoundException(
                "源 TDDFT 输入文件不存在。",
                request.SourceInputPath);
        }

        if (request.TransitionIndex <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.TransitionIndex),
                "激发态编号必须大于零。");
        }

        if (string.IsNullOrWhiteSpace(
                request.SourceCheckpointFileName)
            || !IsFileName(
                request.SourceCheckpointFileName))
        {
            throw new ArgumentException(
                "源 TDDFT 检查点文件名无效。",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
                request.CheckpointFileName)
            || !IsFileName(request.CheckpointFileName))
        {
            throw new ArgumentException(
                "NTO 检查点文件名无效。",
                nameof(request));
        }

        string inputText = await File.ReadAllTextAsync(
            request.SourceInputPath,
            cancellationToken);
        string[] lines = inputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);
        string routeLine = string.Empty;
        List<string> memoryLines = new List<string>();

        for (int index = 0;
            index < lines.Length;
            index++)
        {
            string trimmed = lines[index].Trim();

            if (trimmed.StartsWith(
                    "%mem=",
                    StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(
                    "%nprocshared=",
                    StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith(
                    "%nproc=",
                    StringComparison.OrdinalIgnoreCase))
            {
                memoryLines.Add(trimmed);
                continue;
            }

            if (routeLine.Length == 0
                && trimmed.StartsWith(
                    "#",
                    StringComparison.Ordinal))
            {
                routeLine = BuildNtoRoute(
                    trimmed,
                    request.TransitionIndex);
            }
        }

        if (routeLine.Length == 0)
        {
            throw new InvalidOperationException(
                "源 TDDFT 输入中没有找到 route line。");
        }

        List<string> outputLines = new List<string>();
        outputLines.Add(
            "%oldchk=" +
            request.SourceCheckpointFileName);
        outputLines.Add(
            "%chk=" + request.CheckpointFileName);
        outputLines.AddRange(memoryLines);
        outputLines.Add(string.Empty);
        outputLines.Add(routeLine);
        outputLines.Add(string.Empty);

        string? directory = Path.GetDirectoryName(
            request.TargetInputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            request.TargetInputPath,
            string.Join(Environment.NewLine, outputLines) +
                Environment.NewLine,
            cancellationToken);
    }

    private static string BuildNtoRoute(
        string routeLine,
        int transitionIndex)
    {
        string result = TdKeywordPattern.Replace(
            routeLine,
            " ");
        result = Regex.Replace(
            result.Trim(),
            @"\s+",
            " ");

        result +=
            " Geom=AllCheck Guess=(Read,Only) " +
            "Density=(Check,Transition=" +
            transitionIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
            ") Pop=(Minimal,NTO,SaveNTO)";

        if (result.IndexOf(
            "nosymm",
            StringComparison.OrdinalIgnoreCase) < 0)
        {
            result += " nosymm";
        }

        return result;
    }

    private static bool IsFileName(string value)
    {
        return string.Equals(
                Path.GetFileName(value),
                value,
                StringComparison.Ordinal)
            && value.IndexOfAny(
                Path.GetInvalidFileNameChars()) < 0;
    }
}
