using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.Nto;

/// <summary>Gaussian NTO 输入写入器。</summary>
public sealed class GaussianNtoInputWriter
{
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

        string inputText = await File.ReadAllTextAsync(
            request.SourceInputPath,
            cancellationToken);
        string[] lines = inputText.Split(
            new string[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);
        bool routeUpdated = false;

        for (int index = 0;
            index < lines.Length;
            index++)
        {
            string trimmed = lines[index].Trim();

            if (trimmed.StartsWith(
                "%chk=",
                StringComparison.OrdinalIgnoreCase))
            {
                lines[index] =
                    "%chk=" + request.CheckpointFileName;
                continue;
            }

            if (!routeUpdated
                && trimmed.StartsWith(
                    "#",
                    StringComparison.Ordinal))
            {
                lines[index] =
                    AppendNtoKeywords(
                        lines[index],
                        request.TransitionIndex);
                routeUpdated = true;
            }
        }

        if (!routeUpdated)
        {
            throw new InvalidOperationException(
                "源 TDDFT 输入中没有找到 route line。");
        }

        string? directory = Path.GetDirectoryName(
            request.TargetInputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            request.TargetInputPath,
            string.Join(Environment.NewLine, lines) +
                Environment.NewLine,
            cancellationToken);
    }

    private static string AppendNtoKeywords(
        string routeLine,
        int transitionIndex)
    {
        string result = routeLine.TrimEnd();
        string ntoKeywords =
            " Geom=AllCheck Guess=(Read,Only) " +
            "Density=(Check,Transition=" +
            transitionIndex.ToString(
                System.Globalization.CultureInfo.InvariantCulture) +
            ") Pop=(Minimal,NTO,SaveNTO)";

        result = result + ntoKeywords;

        if (result.IndexOf(
            "nosymm",
            StringComparison.OrdinalIgnoreCase) < 0)
        {
            result = result + " nosymm";
        }

        return result;
    }
}
