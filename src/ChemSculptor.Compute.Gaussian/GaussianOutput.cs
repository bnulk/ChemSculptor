namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 专用输出模型。
/// 这是 Gaussian 模块内部的数据结构，不直接交给智能体使用。
/// </summary>
public sealed class GaussianOutput
{
    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>是否找到 Gaussian 正常结束标志。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>是否找到 Gaussian 错误结束标志。</summary>
    public bool ErrorTermination { get; set; }

    /// <summary>最终 SCF 能量。</summary>
    public double? Energy { get; set; }

    /// <summary>能量所属的方法名称，例如 RCAM-B3LYP。</summary>
    public string EnergyMethod { get; set; } = string.Empty;

    /// <summary>错误结束行。</summary>
    public List<string> ErrorMessages { get; set; } = new List<string>();
}
