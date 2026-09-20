namespace ChemSculptor.Compute;

/// <summary>
/// 工作区中固定文件名称。
/// 具体程序相关的输入输出文件由程序适配器决定，不在这里绑定。
/// </summary>
public static class CalculationWorkspacePaths
{
    /// <summary>几何原始文件名称。</summary>
    public const string GeometryOriginalFileName = "original.txt";

    /// <summary>规范几何文件名称。</summary>
    public const string GeometryCanonicalFileName = "canonical.json";

    /// <summary>几何验证报告文件名称。</summary>
    public const string GeometryValidationFileName = "validation.json";

    /// <summary>作业清单文件名称。</summary>
    public const string JobManifestFileName = "manifest.json";

    /// <summary>本次计算使用的坐标文件名称。</summary>
    public const string JobCoordinatesFileName = "molecule.xyz";

    /// <summary>程序输出文件名称。</summary>
    public const string JobOutputFileName = "output.log";

    /// <summary>结果摘要文件名称。</summary>
    public const string JobSummaryFileName = "summary.txt";

    /// <summary>规范结果文件名称。</summary>
    public const string JobResultFileName = "result.json";

    /// <summary>结果验证报告文件名称。</summary>
    public const string JobValidationFileName = "validation.json";
}
