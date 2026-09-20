using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 输入文件生成选项。
/// 内存和检查点路径由调用方提供，不在本项目中硬编码。
/// </summary>
public sealed class GaussianInputOptions
{
    /// <summary>内存设置，例如 4GB。</summary>
    public string Memory { get; set; } = string.Empty;

    /// <summary>并行核数。</summary>
    public int ProcessorCount { get; set; } = CalculationDefaults.DefaultProcessorCount;

    /// <summary>检查点文件路径；为空时根据输入文件路径推导。</summary>
    public string CheckpointFilePath { get; set; } = string.Empty;

    /// <summary>输入文件标题。</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>创建默认选项；内存仍需要调用方设置。</summary>
    public static GaussianInputOptions CreateDefault()
    {
        GaussianInputOptions options = new GaussianInputOptions();
        options.Memory = string.Empty;
        options.ProcessorCount = CalculationDefaults.DefaultProcessorCount;
        options.CheckpointFilePath = string.Empty;
        options.Title = string.Empty;
        return options;
    }
}
