using System.Globalization;

namespace ChemSculptor.Compute;

/// <summary>
/// 计算默认值。
/// 只生成默认计算方案，不执行任何计算。
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

    /// <summary>电子数为偶数时使用的默认自旋多重度。</summary>
    public const int EvenElectronMultiplicity = 1;

    /// <summary>电子数为奇数时使用的默认自旋多重度。</summary>
    public const int OddElectronMultiplicity = 2;

    /// <summary>默认并行核数。</summary>
    public const int DefaultProcessorCount = 4;

    /// <summary>默认自洽场迭代上限。</summary>
    public const int DefaultScfIterationLimit = 200;

    /// <summary>默认几何优化最大循环次数。</summary>
    public const int DefaultOptimizationMaxCycles = 100;

    /// <summary>默认几何优化收敛标准。</summary>
    public const string DefaultOptimizationConvergence = "Default";

    /// <summary>默认不重启已有几何优化。</summary>
    public const bool DefaultOptimizationRestart = false;

    /// <summary>默认不指定几何优化初始 Hessian 来源。</summary>
    public const string DefaultOptimizationInitialHessian = "None";

    /// <summary>默认 TDDFT 激发态数量。</summary>
    public const int DefaultTddftStateCount = 50;

    /// <summary>
    /// 创建默认的单点计算方案。
    /// 坐标和运行状态不在这里设置。
    /// </summary>
    public static CalculationSpec CreateDefaultSinglePoint()
    {
        return CreateDefaultSpec(CalculationTaskType.SinglePoint);
    }

    /// <summary>
    /// 创建默认的几何优化方案。
    /// 坐标和运行状态不在这里设置。
    /// </summary>
    public static CalculationSpec CreateDefaultOptimization()
    {
        CalculationSpec spec =
            CreateDefaultSpec(CalculationTaskType.Optimization);
        spec.ExtraOptions[CalculationOptionKeys.OptimizationMaxCycles] =
            DefaultOptimizationMaxCycles.ToString(
                CultureInfo.InvariantCulture);
        spec.ExtraOptions[CalculationOptionKeys.OptimizationConvergence] =
            DefaultOptimizationConvergence;
        spec.ExtraOptions[CalculationOptionKeys.OptimizationRestart] =
            "false";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationInitialHessian] =
            DefaultOptimizationInitialHessian;
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationConstraintMode] =
            "None";

        AddParameter(
            spec,
            CalculationOptionKeys.OptimizationMaxCycles,
            "优化最大循环次数",
            DefaultOptimizationMaxCycles.ToString(
                CultureInfo.InvariantCulture),
            DefaultOptimizationMaxCycles.ToString(
                CultureInfo.InvariantCulture),
            CalculationRiskLevel.Info,
            false,
            "默认最多执行 100 次几何优化循环。");

        AddParameter(
            spec,
            CalculationOptionKeys.OptimizationConvergence,
            "优化收敛标准",
            DefaultOptimizationConvergence,
            DefaultOptimizationConvergence,
            CalculationRiskLevel.Info,
            false,
            "默认使用优化程序的常规收敛标准。");

        AddParameter(
            spec,
            CalculationOptionKeys.OptimizationRestart,
            "优化重启",
            "false",
            "false",
            CalculationRiskLevel.Warning,
            false,
            "默认从当前几何开始新的优化。");

        AddParameter(
            spec,
            CalculationOptionKeys.OptimizationInitialHessian,
            "优化初始 Hessian",
            DefaultOptimizationInitialHessian,
            DefaultOptimizationInitialHessian,
            CalculationRiskLevel.Info,
            false,
            "默认不指定初始 Hessian 来源。");

        AddParameter(
            spec,
            CalculationOptionKeys.OptimizationConstraintMode,
            "优化约束模式",
            "None",
            "None",
            CalculationRiskLevel.Info,
            false,
            "默认不冻结任何原子。");

        return spec;
    }

    /// <summary>
    /// 创建默认的几何优化和频率联合计算方案。
    /// 参数沿用默认优化方案，任务类型设为 OptimizationFrequency。
    /// </summary>
    public static CalculationSpec CreateDefaultOptimizationFrequency()
    {
        CalculationSpec spec = CreateDefaultOptimization();
        spec.TaskType =
            CalculationTaskType.OptimizationFrequency;

        for (int index = 0;
            index < spec.Parameters.Count;
            index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                "task",
                StringComparison.OrdinalIgnoreCase))
            {
                parameter.CurrentValue =
                    CalculationTaskType
                        .OptimizationFrequency
                        .ToString();
                break;
            }
        }

        return spec;
    }

    /// <summary>
    /// 创建默认的 TDDFT 单点计算方案。
    /// 默认计算 50 个激发态，并关闭对称性。
    /// </summary>
    public static CalculationSpec CreateDefaultTddftSinglePoint()
    {
        CalculationSpec spec =
            CreateDefaultSpec(CalculationTaskType.TdDft);
        spec.ExtraOptions[
            CalculationOptionKeys.Symmetry] = "Ignore";
        spec.ExtraOptions[
            CalculationOptionKeys.TddftStateCount] =
            DefaultTddftStateCount.ToString(
                CultureInfo.InvariantCulture);

        AddParameter(
            spec,
            CalculationOptionKeys.TddftStateCount,
            "TDDFT 激发态数量",
            DefaultTddftStateCount.ToString(
                CultureInfo.InvariantCulture),
            DefaultTddftStateCount.ToString(
                CultureInfo.InvariantCulture),
            CalculationRiskLevel.Info,
            false,
            "默认计算 50 个激发态。");

        return spec;
    }

    private static CalculationSpec CreateDefaultSpec(
        CalculationTaskType taskType)
    {
        CalculationSpec spec = new CalculationSpec();
        spec.TaskType = taskType;
        spec.Program = DefaultProgram;
        spec.Method = DefaultMethod;
        spec.Basis = DefaultBasis;
        spec.Charge = DefaultCharge;
        // 0 表示尚未解析几何，尚未计算总电子数。
        spec.Multiplicity = 0;
        spec.ElectronicStateObjective = ElectronicStateObjective.GroundState;
        spec.TargetMultiplicity = null;
        spec.TargetStateLabel = string.Empty;
        spec.Solvent = string.Empty;
        spec.ExtraOptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        spec.ExtraOptions[CalculationOptionKeys.DispersionCorrection] =
            "GD3BJ";
        spec.ExtraOptions[CalculationOptionKeys.ScfIterationLimit] =
            DefaultScfIterationLimit.ToString(CultureInfo.InvariantCulture);
        spec.ExtraOptions[
            CalculationOptionKeys.WavefunctionStabilityCheck] =
            "Check";
        spec.Parameters = new List<CalculationParameter>();

        AddParameter(
            spec,
            "task",
            "任务类型",
            taskType.ToString(),
            taskType.ToString(),
            CalculationRiskLevel.Info,
            false,
            "默认任务类型由调用方指定。");

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
            "scf-iteration-limit",
            "SCF 迭代上限",
            DefaultScfIterationLimit.ToString(CultureInfo.InvariantCulture),
            DefaultScfIterationLimit.ToString(CultureInfo.InvariantCulture),
            CalculationRiskLevel.Info,
            false,
            "默认允许最多 200 次 SCF 迭代；具体计算程序负责翻译。");

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
            "0",
            "0",
            CalculationRiskLevel.Blocking,
            true,
            "未显式指定时，由总电子数决定：偶数使用 1，奇数使用 2。");

        AddParameter(
            spec,
            "electronic-state-objective",
            "电子态目标",
            ElectronicStateObjective.GroundState.ToString(),
            ElectronicStateObjective.GroundState.ToString(),
            CalculationRiskLevel.Info,
            false,
            "默认寻找当前方法下的最低能量稳定电子态。");

        return spec;
    }

    /// <summary>根据总电子数返回默认自旋多重度。</summary>
    public static int GetDefaultMultiplicityFromElectronCount(
        int electronCount)
    {
        if (electronCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(electronCount),
                "电子数不能小于 0。");
        }

        if (electronCount % 2 == 0)
        {
            return EvenElectronMultiplicity;
        }

        return OddElectronMultiplicity;
    }

    /// <summary>
    /// 在多重度仍为默认值时，根据总电子数写入默认多重度。
    /// 用户或智能体已经指定的值不会被覆盖。
    /// </summary>
    public static bool ApplyDefaultMultiplicityFromElectronCount(
        CalculationSpec spec,
        int electronCount)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        CalculationParameter? parameter =
            FindParameter(spec, "multiplicity");

        if (parameter != null
            && parameter.Source != ParameterSource.Default)
        {
            return false;
        }

        int multiplicity =
            GetDefaultMultiplicityFromElectronCount(electronCount);
        spec.Multiplicity = multiplicity;

        if (parameter != null)
        {
            parameter.CurrentValue =
                multiplicity.ToString(CultureInfo.InvariantCulture);
            parameter.DefaultValue =
                multiplicity.ToString(CultureInfo.InvariantCulture);
            parameter.Source = ParameterSource.System;
            parameter.Description =
                "根据总电子数自动确定：偶数电子使用 1，奇数电子使用 2。";
        }

        return true;
    }

    private static CalculationParameter? FindParameter(
        CalculationSpec spec,
        string name)
    {
        for (int index = 0; index < spec.Parameters.Count; index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return null;
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
