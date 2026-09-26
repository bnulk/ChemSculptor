namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 16 程序适配器配置。
/// </summary>
public sealed class Gaussian16ProgramAdapterOptions
{
    /// <summary>可执行文件名称或完整路径。</summary>
    public string ExecutablePath { get; set; } = "g16";

    /// <summary>Gaussian 可执行文件目录，会作为 GAUSS_EXEDIR 传给子进程。</summary>
    public string ExecutableDirectory { get; set; } = string.Empty;

    /// <summary>默认内存设置。</summary>
    public string Memory { get; set; } = Gaussian16ProgramAdapter.DefaultMemory;

    /// <summary>默认并行核数。</summary>
    public int ProcessorCount { get; set; } = CalculationDefaults.DefaultProcessorCount;

    /// <summary>
    /// 创建默认配置。
    /// 如果 GAUSS_EXEDIR 已存在则沿用；否则从 PATH 中解析 g16.exe 所在目录。
    /// </summary>
    public static Gaussian16ProgramAdapterOptions CreateDefault()
    {
        Gaussian16ProgramAdapterOptions options = new Gaussian16ProgramAdapterOptions();
        options.ExecutablePath = "g16";
        options.Memory = Gaussian16ProgramAdapter.DefaultMemory;
        options.ProcessorCount = CalculationDefaults.DefaultProcessorCount;

        string? existingExecutableDirectory =
            Environment.GetEnvironmentVariable("GAUSS_EXEDIR");

        if (!string.IsNullOrWhiteSpace(existingExecutableDirectory))
        {
            options.ExecutableDirectory = existingExecutableDirectory;
        }
        else
        {
            options.ExecutableDirectory = ResolveExecutableDirectory("g16");
        }

        return options;
    }

    /// <summary>
    /// 从 PATH 中查找可执行文件所在目录。
    /// 找不到时返回空字符串，让子进程继续使用系统原有环境。
    /// </summary>
    private static string ResolveExecutableDirectory(string executableName)
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        string[] directories = path.Split(Path.PathSeparator);
        string fileName = executableName;

        if (!fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName + ".exe";
        }

        for (int index = 0; index < directories.Length; index++)
        {
            string directory = directories[index].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            string candidatePath = Path.Combine(directory, fileName);
            if (File.Exists(candidatePath))
            {
                return directory;
            }
        }

        return string.Empty;
    }
}
