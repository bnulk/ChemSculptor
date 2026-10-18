using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>验证优化最终收敛指标。</summary>
public sealed class OptimizationConvergenceValidator
    : ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    public string Name
    {
        get { return "optimization-convergence-validator"; }
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

    /// <summary>验证优化最终收敛指标。</summary>
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

        OptimizationConvergence? convergence =
            request.OptimizationResult!.FinalConvergence;

        if (convergence == null)
        {
            NumericalCalculationValidationChecks.AddRequiredCheck(
                report,
                "optimization.convergence.present",
                "存在最终收敛指标",
                CalculationValidationScope.Numerical,
                "存在",
                "缺失",
                false,
                "优化结果中没有最终收敛指标。");
            NumericalCalculationValidationChecks.SetReportStatus(
                report);
            return report;
        }

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.convergence.completed",
            "最终收敛指标全部满足",
            CalculationValidationScope.Numerical,
            "true",
            NumericalCalculationValidationChecks
                .GetBooleanText(convergence.Converged),
            convergence.Converged == true,
            "优化最终收敛指标没有全部满足。");

        AddItemCheck(
            report,
            "optimization.convergence.maximum_force",
            "最大力",
            convergence.MaximumForce,
            convergence.MaximumForceThreshold,
            convergence.MaximumForceConverged);

        AddItemCheck(
            report,
            "optimization.convergence.rms_force",
            "RMS 力",
            convergence.RmsForce,
            convergence.RmsForceThreshold,
            convergence.RmsForceConverged);

        AddItemCheck(
            report,
            "optimization.convergence.maximum_displacement",
            "最大位移",
            convergence.MaximumDisplacement,
            convergence.MaximumDisplacementThreshold,
            convergence.MaximumDisplacementConverged);

        AddItemCheck(
            report,
            "optimization.convergence.rms_displacement",
            "RMS 位移",
            convergence.RmsDisplacement,
            convergence.RmsDisplacementThreshold,
            convergence.RmsDisplacementConverged);

        NumericalCalculationValidationChecks.SetReportStatus(
            report);
        return report;
    }

    private static void AddItemCheck(
        CalculationValidationReport report,
        string code,
        string displayName,
        double? value,
        double? threshold,
        bool? converged)
    {
        bool passed = ResolveConvergence(
            value,
            threshold,
            converged);
        string actualValue = GetValueText(
            value,
            threshold);
        string failureMessage = passed
            ? string.Empty
            : displayName + "未达标：" + actualValue + "。";

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            code,
            displayName + "满足优化收敛标准",
            CalculationValidationScope.Numerical,
            "满足阈值",
            actualValue,
            passed,
            failureMessage);
    }

    private static bool ResolveConvergence(
        double? value,
        double? threshold,
        bool? converged)
    {
        if (converged.HasValue)
        {
            return converged.Value;
        }

        if (value.HasValue && threshold.HasValue)
        {
            return value.Value <= threshold.Value;
        }

        return false;
    }

    private static string GetValueText(
        double? value,
        double? threshold)
    {
        if (!value.HasValue && !threshold.HasValue)
        {
            return "缺失";
        }

        if (!value.HasValue)
        {
            return "数值缺失，阈值 " + FormatValue(threshold);
        }

        if (!threshold.HasValue)
        {
            return FormatValue(value) + "，阈值缺失";
        }

        return FormatValue(value) +
            "，阈值 " +
            FormatValue(threshold);
    }

    private static string FormatValue(double? value)
    {
        if (!value.HasValue)
        {
            return "缺失";
        }

        return value.Value.ToString(
            "G6",
            System.Globalization.CultureInfo.InvariantCulture);
    }
}
