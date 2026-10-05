namespace ChemSculptor.Anomaly.Models;

/// <summary>通用异常检查请求。</summary>
public sealed class AnomalyCheckRequest
{
    /// <summary>请求执行的检查代码。</summary>
    public string CheckCode { get; set; } = string.Empty;

    /// <summary>异常检查上下文。</summary>
    public AnomalyContext Context { get; set; } = new AnomalyContext();
}
