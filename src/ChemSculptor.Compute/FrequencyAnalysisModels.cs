namespace ChemSculptor.Compute;

/// <summary>振动分析结果。</summary>
public sealed class FrequencyAnalysisResult
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>计算程序。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>方法或泛函。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>基组。</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>是否正常结束。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>解析出的振动频率。</summary>
    public List<double> Frequencies { get; set; } =
        new List<double>();

    /// <summary>最终几何中的原子数量。</summary>
    public int? AtomCount { get; set; }

    /// <summary>最终几何是否为线型分子。</summary>
    public bool? IsLinear { get; set; }

    /// <summary>按 3N-5 或 3N-6 计算的期望振动模式数。</summary>
    public int? ExpectedFrequencyCount { get; set; }

    /// <summary>解析出的模式数是否等于期望模式数。</summary>
    public bool? FrequencyCountMatchesExpected { get; set; }

    /// <summary>虚频数量。</summary>
    public int ImaginaryFrequencyCount { get; set; }

    /// <summary>最低频率。</summary>
    public double? MinimumFrequency { get; set; }

    /// <summary>是否为稳定最小点。</summary>
    public bool? IsMinimum { get; set; }

    /// <summary>零点校正。</summary>
    public double? ZeroPointCorrection { get; set; }

    /// <summary>热能校正。</summary>
    public double? ThermalCorrectionToEnergy { get; set; }

    /// <summary>焓校正。</summary>
    public double? ThermalCorrectionToEnthalpy { get; set; }

    /// <summary>吉布斯自由能校正。</summary>
    public double? ThermalCorrectionToGibbs { get; set; }

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } =
        new List<CalculationDiagnostic>();

    /// <summary>产物清单。</summary>
    public List<CalculationArtifactDescriptor> Artifacts { get; set; } =
        new List<CalculationArtifactDescriptor>();
}

/// <summary>程序可选的振动分析能力。</summary>
public interface IFrequencyAnalysisProgram
{
    /// <summary>写入振动分析输入。</summary>
    Task WriteFrequencyInputAsync(
        CalculationSpec spec,
        string sourceInputPath,
        string targetInputPath,
        string checkpointFileName,
        CancellationToken cancellationToken = default);

    /// <summary>解析振动分析输出。</summary>
    Task<FrequencyAnalysisResult> ParseFrequencyOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default);
}
