namespace ChemSculptor.Compute.Local;

/// <summary>
/// 本机进程执行后端配置。
/// </summary>
public sealed class LocalProcessBackendOptions
{
    /// <summary>默认可执行文件路径；为空时必须由执行上下文提供。</summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>标准输出日志文件名。</summary>
    public string StandardOutputFileName { get; set; } = "stdout.log";

    /// <summary>标准错误日志文件名。</summary>
    public string StandardErrorFileName { get; set; } = "stderr.log";

    /// <summary>取消时是否终止整个进程树。</summary>
    public bool KillProcessTreeOnCancel { get; set; } = true;

    /// <summary>创建默认配置。</summary>
    public static LocalProcessBackendOptions CreateDefault()
    {
        LocalProcessBackendOptions options = new LocalProcessBackendOptions();
        options.ExecutablePath = string.Empty;
        options.StandardOutputFileName = "stdout.log";
        options.StandardErrorFileName = "stderr.log";
        options.KillProcessTreeOnCancel = true;
        return options;
    }
}
