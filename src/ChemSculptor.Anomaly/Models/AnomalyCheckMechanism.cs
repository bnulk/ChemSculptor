namespace ChemSculptor.Anomaly.Models;

/// <summary>异常检查获取证据的方式。</summary>
public enum AnomalyCheckMechanism
{
    /// <summary>尚未确定。</summary>
    Unknown,

    /// <summary>解析主计算产生的输出文件。</summary>
    OutputArtifact,

    /// <summary>读取进程、资源和运行环境信号。</summary>
    RuntimeSignal,

    /// <summary>需要执行额外的辅助计算。</summary>
    AuxiliaryCalculation
}
