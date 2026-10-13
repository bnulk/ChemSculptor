using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>Gaussian 优化步骤的收敛指标。</summary>
public sealed class GaussianOptimizationConvergence
{
    /// <summary>是否全部满足收敛条件；没有报告时为空。</summary>
    public bool? Converged { get; set; }

    /// <summary>优化收敛标准。</summary>
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

/// <summary>Gaussian 优化轨迹中的一个步骤。</summary>
public sealed class GaussianOptimizationStep
{
    /// <summary>步骤编号，从 1 开始。</summary>
    public int StepNumber { get; set; }

    /// <summary>计算该步骤时使用的几何结构。</summary>
    public CanonicalGeometry Geometry { get; set; } =
        new CanonicalGeometry();

    /// <summary>步骤能量。</summary>
    public double? Energy { get; set; }

    /// <summary>SCF 是否收敛。</summary>
    public bool? ScfConverged { get; set; }

    /// <summary>最终 SCF 迭代次数。</summary>
    public int? ScfIterations { get; set; }

    /// <summary>该步骤的收敛指标。</summary>
    public GaussianOptimizationConvergence? Convergence
    {
        get;
        set;
    }
}

/// <summary>
/// Gaussian 专用输出模型。
/// 这是 Gaussian 模块内部的数据结构，不直接交给智能体使用。
/// </summary>
public sealed class GaussianOutput
{
    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>是否找到 Gaussian 正常结束标志。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>是否找到 Gaussian 错误结束标志。</summary>
    public bool ErrorTermination { get; set; }

    /// <summary>最终 SCF 能量。</summary>
    public double? Energy { get; set; }

    /// <summary>能量所属的方法名称，例如 RCAM-B3LYP。</summary>
    public string EnergyMethod { get; set; } = string.Empty;

    /// <summary>SCF 是否收敛。</summary>
    public bool? ScfConverged { get; set; }

    /// <summary>最终 SCF 迭代次数。</summary>
    public int? ScfIterations { get; set; }

    /// <summary>错误结束行。</summary>
    public List<string> ErrorMessages { get; set; } = new List<string>();

    /// <summary>是否找到优化完成标志。</summary>
    public bool OptimizationCompleted { get; set; }

    /// <summary>最后解析到的优化步骤编号。</summary>
    public int? LastStepNumber { get; set; }

    /// <summary>优化允许的最大步骤数。</summary>
    public int? MaximumOptimizationCycles { get; set; }

    /// <summary>最终几何结构。</summary>
    public CanonicalGeometry? FinalGeometry { get; set; }

    /// <summary>最终收敛指标。</summary>
    public GaussianOptimizationConvergence? FinalConvergence
    {
        get;
        set;
    }

    /// <summary>优化轨迹。</summary>
    public List<GaussianOptimizationStep> OptimizationSteps
    {
        get;
        set;
    } = new List<GaussianOptimizationStep>();
}
