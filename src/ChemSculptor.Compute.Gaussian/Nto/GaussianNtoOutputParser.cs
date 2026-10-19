using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian.Nto;

/// <summary>Gaussian NTO 输出解析器。</summary>
public sealed class GaussianNtoOutputParser
{
    /// <summary>解析 NTO 输出。</summary>
    public async Task<NtoTransitionResult> ParseAsync(
        string outputPath,
        NtoSelectedState selectedState,
        CancellationToken cancellationToken = default)
    {
        NtoTransitionResult result =
            new NtoTransitionResult();
        result.OutputFilePath = outputPath;
        result.StateIndex = selectedState.StateIndex;
        result.StateLabel = selectedState.StateLabel;

        if (string.IsNullOrWhiteSpace(outputPath)
            || !File.Exists(outputPath))
        {
            result.Error = "NTO 输出文件不存在。";
            return result;
        }

        string text = await File.ReadAllTextAsync(
            outputPath,
            cancellationToken);
        result.NormalTermination =
            text.IndexOf(
                "Normal termination",
                StringComparison.OrdinalIgnoreCase) >= 0;
        result.NtoFound = ContainsNtoOutputMarker(text);
        result.Succeeded =
            result.NormalTermination && result.NtoFound;

        if (!result.NormalTermination)
        {
            result.Error = "NTO 作业没有正常结束。";
        }
        else if (!result.NtoFound)
        {
            result.Error = "NTO 作业没有解析到自然过渡轨道输出。";
        }
        else
        {
            result.Message = "NTO 作业完成。";
        }

        return result;
    }

    private static bool ContainsNtoOutputMarker(string text)
    {
        return text.IndexOf(
                "Natural Transition Orbitals",
                StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf(
                "Natural Transition Orbital",
                StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf(
                "Saving Alpha spin NTOs",
                StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf(
                "Saving Beta spin NTOs",
                StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf(
                "Populations using transition density",
                StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
