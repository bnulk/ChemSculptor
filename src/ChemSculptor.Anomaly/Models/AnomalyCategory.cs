namespace ChemSculptor.Anomaly.Models;

/// <summary>异常的一级分类。</summary>
public enum AnomalyCategory
{
    /// <summary>尚未分类。</summary>
    Unknown,

    /// <summary>进程、资源、环境、文件或基础设施异常。</summary>
    Execution,

    /// <summary>数值算法、科学合理性或结果一致性异常。</summary>
    Scientific
}
