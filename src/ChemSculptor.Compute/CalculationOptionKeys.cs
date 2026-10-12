namespace ChemSculptor.Compute;

/// <summary>通用计算方案中的扩展选项键。</summary>
public static class CalculationOptionKeys
{
    /// <summary>计算引擎。</summary>
    public const string Engine = "engine";

    /// <summary>计算方法或泛函。</summary>
    public const string Method = "method";

    /// <summary>计算基组。</summary>
    public const string Basis = "basis";

    /// <summary>色散校正。</summary>
    public const string DispersionCorrection = "dispersion-correction";

    /// <summary>对称性模式。</summary>
    public const string Symmetry = "symmetry";

    /// <summary>Gaussian route 输出信息量。</summary>
    public const string RoutePrintLevel = "route-print-level";

    /// <summary>自洽场迭代上限。</summary>
    public const string ScfIterationLimit = "scf-iteration-limit";

    /// <summary>溶剂。</summary>
    public const string Solvent = "solvent";

    /// <summary>总电荷。</summary>
    public const string Charge = "charge";

    /// <summary>自旋多重度。</summary>
    public const string Multiplicity = "multiplicity";
}
