namespace ChemSculptor.Anomaly.Models;

/// <summary>振动分析异常代码。</summary>
public static class FrequencyAnalysisAnomalyCodes
{
    /// <summary>振动分析没有正常结束。</summary>
    public const string NotNormalTermination =
        "frequency.not_normal_termination";

    /// <summary>输出中没有振动频率。</summary>
    public const string MissingFrequencies =
        "frequency.missing_frequencies";

    /// <summary>存在虚频，结构不是稳定最小点。</summary>
    public const string ImaginaryModesPresent =
        "frequency.imaginary_modes_present";
}
