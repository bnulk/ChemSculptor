namespace ChemSculptor.Compute;

/// <summary>
/// 计算默认值。
/// 第一阶段只提供默认单点方案生成，不执行任何计算。
/// </summary>
public static class CalculationDefaults
{
    /// <summary>默认计算程序。</summary>
    public const string DefaultProgram = "Gaussian 16";

    /// <summary>默认方法。</summary>
    public const string DefaultMethod = "CAM-B3LYP";

    /// <summary>默认基组。</summary>
    public const string DefaultBasis = "6-31G*";

    /// <summary>默认电荷。</summary>
    public const int DefaultCharge = 0;

    /// <summary>默认自旋多重度。</summary>
    public const int DefaultMultiplicity = 1;

    /// <summary>默认并行核数。</summary>
    public const int DefaultProcessorCount = 4;

    /// <summary>
    /// 创建默认的单点计算方案。
    /// 坐标和运行状态不在这里设置。
    /// </summary>
    public static CalculationSpec CreateDefaultSinglePoint()
    {
        CalculationSpec spec = new CalculationSpec();
        spec.TaskType = CalculationTaskType.SinglePoint;
        spec.Program = DefaultProgram;
        spec.Method = DefaultMethod;
        spec.Basis = DefaultBasis;
        spec.Charge = DefaultCharge;
        spec.Multiplicity = DefaultMultiplicity;
        spec.Solvent = string.Empty;
        spec.ExtraOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        spec.Parameters = new List<CalculationParameter>();

        AddParameter(
            spec,
            "task",
            "任务类型",
            CalculationTaskType.SinglePoint.ToString(),
            CalculationTaskType.SinglePoint.ToString(),
            CalculationRiskLevel.Info,
            false,
            "当前阶段默认执行单点能计算。");

        AddParameter(
            spec,
            "program",
            "计算程序",
            DefaultProgram,
            DefaultProgram,
            CalculationRiskLevel.Info,
            false,
            "当前阶段默认使用 Gaussian 16。");

        AddParameter(
            spec,
            "method",
            "方法",
            DefaultMethod,
            DefaultMethod,
            CalculationRiskLevel.Info,
            false,
            "默认使用 CAM-B3LYP。");

        AddParameter(
            spec,
            "basis",
            "基组",
            DefaultBasis,
            DefaultBasis,
            CalculationRiskLevel.Info,
            false,
            "默认使用 6-31G*。");

        AddParameter(
            spec,
            "charge",
            "总电荷",
            DefaultCharge.ToString(),
            DefaultCharge.ToString(),
            CalculationRiskLevel.Warning,
            true,
            "电荷改变会影响分子身份，需要用户确认。");

        AddParameter(
            spec,
            "multiplicity",
            "自旋多重度",
            DefaultMultiplicity.ToString(),
            DefaultMultiplicity.ToString(),
            CalculationRiskLevel.Blocking,
            true,
            "多重度改变可能改变电子态，需要用户明确确认。");

        return spec;
    }

    private static void AddParameter(
        CalculationSpec spec,
        string name,
        string displayName,
        string currentValue,
        string defaultValue,
        CalculationRiskLevel riskLevel,
        bool requiresApproval,
        string description)
    {
        CalculationParameter parameter = new CalculationParameter();
        parameter.Name = name;
        parameter.DisplayName = displayName;
        parameter.CurrentValue = currentValue;
        parameter.DefaultValue = defaultValue;
        parameter.Source = ParameterSource.Default;
        parameter.RiskLevel = riskLevel;
        parameter.RequiresApproval = requiresApproval;
        parameter.Description = description;
        spec.Parameters.Add(parameter);
    }
}
