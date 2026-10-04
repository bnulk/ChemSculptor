namespace ChemSculptor.Anomaly.Models;

/// <summary>异常的严重程度。</summary>
public enum AnomalySeverity
{
    /// <summary>普通信息。</summary>
    Info,

    /// <summary>需要注意，但不一定阻止计算。</summary>
    Warning,

    /// <summary>错误，通常需要修正。</summary>
    Error,

    /// <summary>阻塞异常，必须处理后才能继续。</summary>
    Blocking
}
