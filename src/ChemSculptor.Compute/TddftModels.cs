namespace ChemSculptor.Compute;

/// <summary>TDDFT 单点计算结果。</summary>
public sealed class TddftCalculationResult
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

    /// <summary>基态能量。</summary>
    public double? GroundStateEnergy { get; set; }

    /// <summary>激发态列表。</summary>
    public List<TddftExcitedState> ExcitedStates { get; set; } =
        new List<TddftExcitedState>();

    /// <summary>跃迁速度四极矩记录。</summary>
    public List<TddftTransitionMoment> TransitionVelocityQuadrupoleMoments
    {
        get;
        set;
    } = new List<TddftTransitionMoment>();

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } =
        new List<CalculationDiagnostic>();

    /// <summary>产物清单。</summary>
    public List<CalculationArtifactDescriptor> Artifacts { get; set; } =
        new List<CalculationArtifactDescriptor>();
}

/// <summary>TDDFT 激发态。</summary>
public sealed class TddftExcitedState
{
    /// <summary>激发态编号。</summary>
    public int StateIndex { get; set; }

    /// <summary>电子态标签，例如 Singlet-?Sym。</summary>
    public string StateLabel { get; set; } = string.Empty;

    /// <summary>激发能，单位 eV。</summary>
    public double EnergyEv { get; set; }

    /// <summary>波长，单位 nm。</summary>
    public double WavelengthNm { get; set; }

    /// <summary>振子强度。</summary>
    public double OscillatorStrength { get; set; }

    /// <summary>S 平方期望值。</summary>
    public double S2 { get; set; }

    /// <summary>轨道跃迁贡献。</summary>
    public List<TddftOrbitalTransition> Transitions { get; set; } =
        new List<TddftOrbitalTransition>();
}

/// <summary>TDDFT 轨道跃迁贡献。</summary>
public sealed class TddftOrbitalTransition
{
    /// <summary>起始轨道。</summary>
    public int FromOrbital { get; set; }

    /// <summary>目标轨道。</summary>
    public int ToOrbital { get; set; }

    /// <summary>跃迁系数。</summary>
    public double Coefficient { get; set; }
}

/// <summary>TDDFT 跃迁矩。</summary>
public sealed class TddftTransitionMoment
{
    /// <summary>激发态编号。</summary>
    public int StateIndex { get; set; }

    /// <summary>解析出的分量。</summary>
    public List<TddftTransitionMomentComponent> Components
    {
        get;
        set;
    } = new List<TddftTransitionMomentComponent>();

    /// <summary>原始表格行。</summary>
    public string RawText { get; set; } = string.Empty;
}

/// <summary>TDDFT 跃迁矩的一个分量。</summary>
public sealed class TddftTransitionMomentComponent
{
    /// <summary>分量名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>分量数值。</summary>
    public double Value { get; set; }
}

/// <summary>程序可选的 TDDFT 输出解析能力。</summary>
public interface ITddftOutputParser
{
    /// <summary>解析 TDDFT 输出。</summary>
    Task<TddftCalculationResult> ParseTddftOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default);
}
