using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>
/// 单点和优化步骤共用的数值验证检查。
/// </summary>
internal static class NumericalCalculationValidationChecks
{
    /// <summary>添加 SCF 收敛检查。</summary>
    public static void AddScfConvergenceCheck(
        CalculationValidationReport report,
        string code,
        string description,
        bool? scfConverged)
    {
        AddRequiredCheck(
            report,
            code,
            description,
            CalculationValidationScope.Numerical,
            "true",
            GetBooleanText(scfConverged),
            scfConverged == true,
            "SCF 没有收敛，或输出中没有可确认的收敛信息。");
    }

    /// <summary>添加能量存在检查。</summary>
    public static void AddEnergyPresentCheck(
        CalculationValidationReport report,
        string code,
        string description,
        double? energy)
    {
        AddRequiredCheck(
            report,
            code,
            description,
            CalculationValidationScope.Numerical,
            "存在",
            GetEnergyText(energy),
            energy.HasValue,
            "结果中没有能量。");
    }

    /// <summary>添加能量有限值检查。</summary>
    public static void AddEnergyFiniteCheck(
        CalculationValidationReport report,
        string code,
        string description,
        double? energy)
    {
        AddRequiredCheck(
            report,
            code,
            description,
            CalculationValidationScope.Numerical,
            "有限数值",
            GetEnergyText(energy),
            energy.HasValue
                && double.IsFinite(energy.Value),
            "能量不是有限数值。");
    }

    /// <summary>添加一项必需检查。</summary>
    public static void AddRequiredCheck(
        CalculationValidationReport report,
        string code,
        string description,
        CalculationValidationScope scope,
        string expectedValue,
        string actualValue,
        bool passed,
        string failureMessage)
    {
        CalculationValidationCheck check =
            new CalculationValidationCheck();
        check.Code = code;
        check.Description = description;
        check.Passed = passed;
        check.Severity = CalculationDiagnosticSeverity.Error;
        check.Requirement = CalculationValidationRequirement.Required;
        check.Scope = scope;
        check.ExpectedValue = expectedValue;
        check.ActualValue = actualValue;

        if (passed)
        {
            check.Message = "通过。";
        }
        else
        {
            check.Message = failureMessage;
            CalculationValidationIssue issue =
                new CalculationValidationIssue();
            issue.Severity = CalculationDiagnosticSeverity.Error;
            issue.Code = code;
            issue.Message = failureMessage;
            report.Issues.Add(issue);
        }

        report.Checks.Add(check);
    }

    /// <summary>添加一项建议检查。</summary>
    public static void AddRecommendedCheck(
        CalculationValidationReport report,
        string code,
        string description,
        CalculationValidationScope scope,
        string expectedValue,
        string actualValue,
        bool passed,
        string failureMessage)
    {
        CalculationValidationCheck check =
            new CalculationValidationCheck();
        check.Code = code;
        check.Description = description;
        check.Passed = passed;
        check.Severity = CalculationDiagnosticSeverity.Warning;
        check.Requirement =
            CalculationValidationRequirement.Recommended;
        check.Scope = scope;
        check.ExpectedValue = expectedValue;
        check.ActualValue = actualValue;

        if (passed)
        {
            check.Message = "通过。";
        }
        else
        {
            check.Message = failureMessage;
            CalculationValidationIssue issue =
                new CalculationValidationIssue();
            issue.Severity = CalculationDiagnosticSeverity.Warning;
            issue.Code = code;
            issue.Message = failureMessage;
            report.Issues.Add(issue);
        }

        report.Checks.Add(check);
    }

    /// <summary>获取能量显示文本。</summary>
    public static string GetEnergyText(double? energy)
    {
        if (!energy.HasValue)
        {
            return "缺失";
        }

        return energy.Value.ToString(
            "G17",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>获取布尔值显示文本。</summary>
    public static string GetBooleanText(bool? value)
    {
        if (!value.HasValue)
        {
            return "未知";
        }

        return value.Value ? "true" : "false";
    }

    /// <summary>根据检查结果设置报告状态。</summary>
    public static void SetReportStatus(
        CalculationValidationReport report)
    {
        bool hasError = false;
        bool hasWarning = false;

        for (int index = 0;
            index < report.Checks.Count;
            index++)
        {
            CalculationValidationCheck check =
                report.Checks[index];

            if (check.Passed)
            {
                continue;
            }

            if (check.Requirement
                == CalculationValidationRequirement.Required)
            {
                hasError = true;
            }
            else if (check.Requirement
                == CalculationValidationRequirement.Recommended)
            {
                hasWarning = true;
            }
        }

        if (hasError)
        {
            report.Passed = false;
            report.Status = CalculationValidationStatus.Failed;
            return;
        }

        report.Passed = true;

        if (hasWarning)
        {
            report.Status =
                CalculationValidationStatus.PassedWithWarnings;
        }
        else
        {
            report.Status = CalculationValidationStatus.Passed;
        }
    }
}
