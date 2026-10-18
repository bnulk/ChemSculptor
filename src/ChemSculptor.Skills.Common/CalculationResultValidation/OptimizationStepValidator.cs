using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>
/// 优化步骤验证器。
/// 逐步复用单点数值验证，检查每个优化步骤是否存在、SCF 是否收敛、
/// 能量是否存在且为有限值，以及步骤几何是否存在。
/// </summary>
public sealed class OptimizationStepValidator
    : ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    public string Name
    {
        get { return "optimization-step-validator"; }
    }

    /// <summary>只处理包含优化结果的优化任务。</summary>
    public bool CanValidate(
        CalculationResultValidationRequest request)
    {
        if (request == null)
        {
            return false;
        }

        return (request.Job.Spec.TaskType
                == CalculationTaskType.Optimization
            || request.Job.Spec.TaskType
                == CalculationTaskType.OptimizationFrequency)
            && request.OptimizationResult != null;
    }

    /// <summary>验证全部优化步骤。</summary>
    public CalculationValidationReport Validate(
        CalculationResultValidationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        CalculationValidationReport report =
            new CalculationValidationReport();
        report.ValidatorName = Name;
        report.ValidatedAt = DateTimeOffset.UtcNow;

        OptimizationResult result = request.OptimizationResult!;
        if (result.Trajectory.Count == 0)
        {
            NumericalCalculationValidationChecks.AddRequiredCheck(
                report,
                "optimization.steps_present",
                "优化输出中至少包含一个步骤",
                CalculationValidationScope.Structure,
                "至少一个步骤",
                "0",
                false,
                "优化输出中没有可解析的步骤。");
            NumericalCalculationValidationChecks.SetReportStatus(
                report);
            return report;
        }

        for (int index = 0;
            index < result.Trajectory.Count;
            index++)
        {
            AddStepChecks(
                report,
                result.Trajectory[index]);
        }

        NumericalCalculationValidationChecks.SetReportStatus(
            report);
        return report;
    }

    private static void AddStepChecks(
        CalculationValidationReport report,
        OptimizationStep step)
    {
        string prefix =
            "optimization.step." +
            step.StepIndex.ToString() +
            ".";
        string displayIndex =
            step.StepIndex.ToString();

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            prefix + "geometry_present",
            "优化步骤 " + displayIndex + " 存在几何结构",
            CalculationValidationScope.Structure,
            "至少一个原子",
            step.Geometry == null
                ? "缺失"
                : step.Geometry.Atoms.Count.ToString(),
            step.Geometry != null
                && step.Geometry.Atoms.Count > 0,
            "优化步骤 " + displayIndex + " 没有几何结构。");

        NumericalCalculationValidationChecks
            .AddScfConvergenceCheck(
                report,
                prefix + "scf_converged",
                "优化步骤 " + displayIndex + " 的 SCF 已收敛",
                step.ScfConverged);

        NumericalCalculationValidationChecks
            .AddEnergyPresentCheck(
                report,
                prefix + "energy_present",
                "优化步骤 " + displayIndex + " 存在能量",
                step.Energy);

        NumericalCalculationValidationChecks
            .AddEnergyFiniteCheck(
                report,
                prefix + "energy_finite",
                "优化步骤 " + displayIndex + " 的能量为有限值",
                step.Energy);
    }
}
