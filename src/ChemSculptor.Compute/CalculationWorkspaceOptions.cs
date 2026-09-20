namespace ChemSculptor.Compute;

/// <summary>
/// 计算工作区配置。
/// </summary>
public sealed class CalculationWorkspaceOptions
{
    /// <summary>工作区根目录。</summary>
    public string RootDirectory { get; set; } = string.Empty;

    /// <summary>几何资产目录名称。</summary>
    public string GeometriesDirectoryName { get; set; } = "geometries";

    /// <summary>计算作业目录名称。</summary>
    public string JobsDirectoryName { get; set; } = "jobs";

    /// <summary>输入文件目录名称。</summary>
    public string InputDirectoryName { get; set; } = "input";

    /// <summary>运行目录名称。</summary>
    public string RunDirectoryName { get; set; } = "run";

    /// <summary>结果目录名称。</summary>
    public string ResultsDirectoryName { get; set; } = "results";

    /// <summary>
    /// 创建默认工作区配置。
    /// Windows 默认使用 %ProgramData%\ChemSculptor。
    /// </summary>
    public static CalculationWorkspaceOptions CreateDefault()
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();

        string commonApplicationData =
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        if (string.IsNullOrWhiteSpace(commonApplicationData))
        {
            commonApplicationData = Path.GetTempPath();
        }

        options.RootDirectory = Path.Combine(commonApplicationData, "ChemSculptor");

        return options;
    }
}
