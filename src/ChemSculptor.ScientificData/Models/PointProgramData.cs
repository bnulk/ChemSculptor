namespace ChemSculptor.ScientificData.Models;

/// <summary>
/// 程序专门数据。
/// 通用科学模型不解释其中的内容，由对应程序模块负责读写。
/// </summary>
public sealed class PointProgramData
{
    /// <summary>程序代码，例如 gaussian 或 orca。</summary>
    public string ProgramCode { get; set; } = string.Empty;

    /// <summary>程序版本。</summary>
    public string ProgramVersion { get; set; } = string.Empty;

    /// <summary>输入格式。</summary>
    public string InputFormat { get; set; } = string.Empty;

    /// <summary>程序数据模式版本。</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>程序专门字段。</summary>
    public Dictionary<string, string> Values { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
