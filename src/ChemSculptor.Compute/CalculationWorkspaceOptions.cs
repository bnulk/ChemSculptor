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
    /// 首选 %ProgramData%\ChemSculptor；
    /// 如果当前账户没有写权限，则回退到当前用户的 LocalAppData。
    /// </summary>
    public static CalculationWorkspaceOptions CreateDefault()
    {
        CalculationWorkspaceOptions options = new CalculationWorkspaceOptions();

        string commonApplicationData =
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        if (!string.IsNullOrWhiteSpace(commonApplicationData))
        {
            string programDataRoot = Path.Combine(commonApplicationData, "ChemSculptor");

            if (IsDirectoryWritable(programDataRoot))
            {
                options.RootDirectory = programDataRoot;
                return options;
            }
        }

        string localApplicationData =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (!string.IsNullOrWhiteSpace(localApplicationData))
        {
            string localRoot = Path.Combine(localApplicationData, "ChemSculptor");

            if (IsDirectoryWritable(localRoot))
            {
                options.RootDirectory = localRoot;
                return options;
            }
        }

        // 受限沙箱或权限异常时回退到临时目录，保证调试流程仍可运行。
        options.RootDirectory = Path.Combine(Path.GetTempPath(), "ChemSculptor");

        return options;
    }

    /// <summary>
    /// 检查目录是否可写。
    /// 通过创建并删除一个临时文件进行探测，不修改已有数据。
    /// </summary>
    private static bool IsDirectoryWritable(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);

            string probeFileName = "write-test-" + Guid.NewGuid().ToString("N") + ".tmp";
            string probePath = Path.Combine(directory, probeFileName);

            using (FileStream stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                stream.WriteByte(0);
            }

            File.Delete(probePath);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
