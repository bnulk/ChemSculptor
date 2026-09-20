using System.Globalization;
using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 输入文件生成器。
/// 当前阶段只支持单点计算输入，不负责启动程序或解析输出。
/// </summary>
public sealed class GaussianInputWriter
{
    /// <summary>
    /// 生成 Gaussian 输入文件。
    /// 输入内容由计算方案、规范几何和运行选项共同决定。
    /// </summary>
    public async Task WriteAsync(
        CalculationSpec spec,
        CanonicalGeometry geometry,
        GaussianInputOptions options,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(spec, geometry, options, outputPath);

        string checkpointPath = options.CheckpointFilePath;
        if (string.IsNullOrWhiteSpace(checkpointPath))
        {
            checkpointPath = Path.ChangeExtension(outputPath, ".chk");
        }

        string routeKeyword = GetRouteKeyword(spec.TaskType);
        string title = options.Title;

        if (string.IsNullOrWhiteSpace(title))
        {
            title = "ChemSculptor calculation";
        }

        using (StreamWriter writer = new StreamWriter(outputPath, false))
        {
            await writer.WriteLineAsync("%chk=" + checkpointPath);
            await writer.WriteLineAsync("%mem=" + options.Memory);
            await writer.WriteLineAsync(
                "%nprocshared=" + options.ProcessorCount.ToString(CultureInfo.InvariantCulture));
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(
                "#p " + spec.Method + "/" + spec.Basis + " " + routeKeyword);
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(title);
            await writer.WriteLineAsync();
            await writer.WriteLineAsync(
                spec.Charge.ToString(CultureInfo.InvariantCulture) +
                " " +
                spec.Multiplicity.ToString(CultureInfo.InvariantCulture));

            for (int index = 0; index < geometry.Atoms.Count; index++)
            {
                CanonicalAtom atom = geometry.Atoms[index];
                await writer.WriteLineAsync(FormatAtomLine(atom));
            }

            await writer.WriteLineAsync();
        }
    }

    private static void ValidateArguments(
        CalculationSpec spec,
        CanonicalGeometry geometry,
        GaussianInputOptions options,
        string outputPath)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        if (geometry == null)
        {
            throw new ArgumentNullException(nameof(geometry));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("输出路径不能为空。", nameof(outputPath));
        }

        if (string.IsNullOrWhiteSpace(spec.Method))
        {
            throw new ArgumentException("计算方法不能为空。", nameof(spec));
        }

        if (string.IsNullOrWhiteSpace(spec.Basis))
        {
            throw new ArgumentException("计算基组不能为空。", nameof(spec));
        }

        if (geometry.Atoms.Count == 0)
        {
            throw new ArgumentException("几何中没有任何原子。", nameof(geometry));
        }

        if (options.ProcessorCount <= 0)
        {
            throw new ArgumentException("并行核数必须大于零。", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Memory))
        {
            throw new ArgumentException("内存设置不能为空。", nameof(options));
        }

        if (spec.Multiplicity <= 0)
        {
            throw new ArgumentException("自旋多重度必须大于零。", nameof(spec));
        }
    }

    private static string GetRouteKeyword(CalculationTaskType taskType)
    {
        if (taskType == CalculationTaskType.SinglePoint)
        {
            return "SP";
        }

        throw new NotSupportedException("当前阶段只支持单点计算输入生成。");
    }

    private static string FormatAtomLine(CanonicalAtom atom)
    {
        return atom.Element +
            " " +
            atom.X.ToString("F6", CultureInfo.InvariantCulture) +
            " " +
            atom.Y.ToString("F6", CultureInfo.InvariantCulture) +
            " " +
            atom.Z.ToString("F6", CultureInfo.InvariantCulture);
    }
}
