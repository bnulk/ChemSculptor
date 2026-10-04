namespace ChemSculptor.Anomaly.Models;

/// <summary>支持异常判断的一条证据。</summary>
public sealed class AnomalyEvidence
{
    /// <summary>证据代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>证据说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>证据来源，例如程序输出、验证报告或稳定性检查。</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>与证据有关的补充信息。</summary>
    public Dictionary<string, string> Details { get; set; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
