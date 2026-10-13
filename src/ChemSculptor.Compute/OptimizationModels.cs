using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Compute;

/// <summary>几何优化失败类别。</summary>
public enum OptimizationFailureKind
{
    /// <summary>没有失败。</summary>
    None,

    /// <summary>计算进程失败。</summary>
    ProcessFailed,

    /// <summary>输出文件不存在。</summary>
    OutputMissing,

    /// <summary>缺少正常结束标志。</summary>
    NormalTerminationMissing,

    /// <summary>计算程序报告错误结束。</summary>
    ProgramError,

    /// <summary>缺少最终能量。</summary>
    EnergyMissing,

    /// <summary>计算已取消。</summary>
    Canceled,

    /// <summary>SCF 没有收敛。</summary>
    ScfNotConverged,

    /// <summary>优化没有收敛。</summary>
    OptimizationNotConverged,

    /// <summary>优化达到最大循环次数。</summary>
    MaxCyclesExceeded,

    /// <summary>力收敛指标未满足。</summary>
    ForceNotConverged,

    /// <summary>位移收敛指标未满足。</summary>
    DisplacementNotConverged,

    /// <summary>优化过程出现振荡。</summary>
    Oscillation,

    /// <summary>优化步骤被拒绝。</summary>
    StepRejected,

    /// <summary>无法分类的失败。</summary>
    Unknown
}

/// <summary>优化收敛指标。</summary>
public sealed class OptimizationConvergence
{
    /// <summary>是否已经收敛；没有报告时为空。</summary>
    public bool? Converged { get; set; }

    /// <summary>收敛标准名称。</summary>
    public string Criteria { get; set; } = "Default";

    /// <summary>最大力。</summary>
    public double? MaximumForce { get; set; }

    /// <summary>最大力是否达标。</summary>
    public bool? MaximumForceConverged { get; set; }

    /// <summary>最大力阈值。</summary>
    public double? MaximumForceThreshold { get; set; }

    /// <summary>均方根力。</summary>
    public double? RmsForce { get; set; }

    /// <summary>均方根力是否达标。</summary>
    public bool? RmsForceConverged { get; set; }

    /// <summary>均方根力阈值。</summary>
    public double? RmsForceThreshold { get; set; }

    /// <summary>最大位移。</summary>
    public double? MaximumDisplacement { get; set; }

    /// <summary>最大位移是否达标。</summary>
    public bool? MaximumDisplacementConverged { get; set; }

    /// <summary>最大位移阈值。</summary>
    public double? MaximumDisplacementThreshold { get; set; }

    /// <summary>均方根位移。</summary>
    public double? RmsDisplacement { get; set; }

    /// <summary>均方根位移是否达标。</summary>
    public bool? RmsDisplacementConverged { get; set; }

    /// <summary>均方根位移阈值。</summary>
    public double? RmsDisplacementThreshold { get; set; }
}

/// <summary>优化轨迹中的一个原子梯度分量。</summary>
public sealed class OptimizationGradientComponent
{
    /// <summary>原子序号，从 1 开始。</summary>
    public int AtomIndex { get; set; }

    /// <summary>元素符号。</summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>X 方向梯度。</summary>
    public double X { get; set; }

    /// <summary>Y 方向梯度。</summary>
    public double Y { get; set; }

    /// <summary>Z 方向梯度。</summary>
    public double Z { get; set; }
}

/// <summary>几何优化轨迹中的一个单点步骤。</summary>
public sealed class OptimizationStep
{
    /// <summary>步骤编号，从 1 开始。</summary>
    public int StepIndex { get; set; }

    /// <summary>该步骤对应的几何结构。</summary>
    public CanonicalGeometry Geometry { get; set; } =
        new CanonicalGeometry();

    /// <summary>该步骤的最终能量。</summary>
    public double? Energy { get; set; }

    /// <summary>能量单位。</summary>
    public string EnergyUnit { get; set; } = "Hartree";

    /// <summary>该步骤的 SCF 是否收敛。</summary>
    public bool? ScfConverged { get; set; }

    /// <summary>该步骤最终 SCF 迭代次数。</summary>
    public int? ScfIterations { get; set; }

    /// <summary>该步骤的原子梯度。</summary>
    public List<OptimizationGradientComponent> Gradients
    {
        get;
        set;
    } = new List<OptimizationGradientComponent>();

    /// <summary>该步骤的收敛指标。</summary>
    public OptimizationConvergence? Convergence { get; set; }

    /// <summary>该步骤的诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } =
        new List<CalculationDiagnostic>();
}

/// <summary>几何优化结果。</summary>
public sealed class OptimizationResult
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>计算程序名称。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>方法或泛函。</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>基组。</summary>
    public string Basis { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int Multiplicity { get; set; }

    /// <summary>程序是否正常结束。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>优化是否收敛。</summary>
    public bool? OptimizationConverged { get; set; }

    /// <summary>优化步骤数量。</summary>
    public int StepCount { get; set; }

    /// <summary>最终能量。</summary>
    public double? FinalEnergy { get; set; }

    /// <summary>能量单位。</summary>
    public string EnergyUnit { get; set; } = "Hartree";

    /// <summary>最终几何结构。</summary>
    public CanonicalGeometry FinalGeometry { get; set; } =
        new CanonicalGeometry();

    /// <summary>最终收敛指标。</summary>
    public OptimizationConvergence? FinalConvergence
    {
        get;
        set;
    }

    /// <summary>优化轨迹。</summary>
    public List<OptimizationStep> Trajectory { get; set; } =
        new List<OptimizationStep>();

    /// <summary>优化失败类别。</summary>
    public OptimizationFailureKind FailureKind { get; set; } =
        OptimizationFailureKind.None;

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } =
        new List<CalculationDiagnostic>();

    /// <summary>本次优化形成的产物清单。</summary>
    public List<CalculationArtifactDescriptor> Artifacts { get; set; } =
        new List<CalculationArtifactDescriptor>();
}

/// <summary>几何优化请求。</summary>
public sealed class OptimizationRequest
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>几何资产标识。</summary>
    public string GeometryId { get; set; } = string.Empty;

    /// <summary>用户自然语言目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>本次优化使用的分子坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>总电荷。</summary>
    public int? Charge { get; set; }

    /// <summary>自旋多重度。</summary>
    public int? Multiplicity { get; set; }

    /// <summary>已经由参数 Resolver 解析完成的计算方案。</summary>
    public CalculationSpec? ResolvedSpec { get; set; }

    /// <summary>已经由参数 Resolver 解析完成的规范参数。</summary>
    public List<ResolvedCalculationParameter> ResolvedParameters
    {
        get;
        set;
    } = new List<ResolvedCalculationParameter>();
}

/// <summary>几何优化提交结果。</summary>
public sealed class OptimizationSubmissionResult
{
    /// <summary>是否成功提交。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>已创建的作业。</summary>
    public CalculationJob? Job { get; set; }

    /// <summary>已完成的优化结果。</summary>
    public OptimizationResult? Result { get; set; }

    /// <summary>结果验证报告。</summary>
    public CalculationValidationReport? Report { get; set; }

    /// <summary>恢复修正次数。</summary>
    public int CorrectionCount { get; set; }

    /// <summary>最终采用的作业标识。</summary>
    public string FinalJobId { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } =
        new List<string>();
}
