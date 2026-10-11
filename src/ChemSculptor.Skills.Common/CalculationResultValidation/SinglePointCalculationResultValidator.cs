using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>
/// 单点计算通用结果验证器。
/// 验证正常结束、能量有效性以及结果与计算方案的一致性。
/// </summary>
public sealed class SinglePointCalculationResultValidator : ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    public string Name
    {
        get { return "single-point-result-validator"; }
    }

    /// <summary>只处理单点计算。</summary>
    public bool CanValidate(CalculationResultValidationRequest request)
    {
        return request.Job.Spec.TaskType == CalculationTaskType.SinglePoint;
    }

    /// <summary>执行单点结果验证。</summary>
    public CalculationValidationReport Validate(
        CalculationResultValidationRequest request)
    {
        CalculationValidationReport report = new CalculationValidationReport();
        report.ValidatorName = Name;
        report.ValidatedAt = DateTimeOffset.UtcNow;

        AddRequiredCheck(
            report,
            "calculation.job_id",
            "结果作业标识与作业一致",
            CalculationValidationScope.Structure,
            request.Job.JobId,
            request.Result.JobId,
            string.Equals(
                request.Job.JobId,
                request.Result.JobId,
                StringComparison.OrdinalIgnoreCase),
            "结果中的作业标识与请求不一致。");

        AddRequiredCheck(
            report,
            "calculation.program",
            "计算程序与方案一致",
            CalculationValidationScope.Structure,
            request.Job.Spec.Program,
            request.Result.Program,
            string.Equals(
                request.Job.Spec.Program,
                request.Result.Program,
                StringComparison.OrdinalIgnoreCase),
            "结果中的计算程序与方案不一致。");

        AddRequiredCheck(
            report,
            "calculation.method",
            "计算方法与方案一致",
            CalculationValidationScope.Structure,
            request.Job.Spec.Method,
            request.Result.Method,
            string.Equals(
                request.Job.Spec.Method,
                request.Result.Method,
                StringComparison.OrdinalIgnoreCase),
            "结果中的计算方法与方案不一致。");

        AddRequiredCheck(
            report,
            "calculation.basis",
            "计算基组与方案一致",
            CalculationValidationScope.Structure,
            request.Job.Spec.Basis,
            request.Result.Basis,
            string.Equals(
                request.Job.Spec.Basis,
                request.Result.Basis,
                StringComparison.OrdinalIgnoreCase),
            "结果中的基组与方案不一致。");

        AddRequiredCheck(
            report,
            "calculation.charge",
            "总电荷与方案一致",
            CalculationValidationScope.Structure,
            request.Job.Spec.Charge.ToString(),
            request.Result.Charge.ToString(),
            request.Job.Spec.Charge == request.Result.Charge,
            "结果中的总电荷与方案不一致。");

        AddRequiredCheck(
            report,
            "calculation.multiplicity",
            "自旋多重度与方案一致",
            CalculationValidationScope.Structure,
            request.Job.Spec.Multiplicity.ToString(),
            request.Result.Multiplicity.ToString(),
            request.Job.Spec.Multiplicity == request.Result.Multiplicity,
            "结果中的自旋多重度与方案不一致。");

        AddRequiredCheck(
            report,
            "calculation.normal_termination",
            "计算正常结束",
            CalculationValidationScope.ProgramOutput,
            "true",
            request.Result.NormalTermination.ToString(),
            request.Result.NormalTermination,
            "计算没有正常结束。");

        AddRequiredCheck(
            report,
            "calculation.scf_converged",
            "SCF 已收敛",
            CalculationValidationScope.Numerical,
            "true",
            GetScfConvergenceText(request.Result),
            request.Result.ScfConverged == true,
            "SCF 没有收敛，或输出中没有可确认的收敛信息。");

        AddRequiredCheck(
            report,
            "calculation.output_normal_termination",
            "输出文件中包含正常终结",
            CalculationValidationScope.ProgramOutput,
            "true",
            request.Result.NormalTermination.ToString(),
            request.Result.NormalTermination,
            "输出结果没有声明正常终结。");

        AddRequiredCheck(
            report,
            "calculation.failure_kind",
            "通用失败类别为 None",
            CalculationValidationScope.Structure,
            CalculationFailureKind.None.ToString(),
            request.Result.FailureKind.ToString(),
            request.Result.FailureKind == CalculationFailureKind.None,
            "计算被标记为失败：" + request.Result.FailureKind.ToString());

        AddRequiredCheck(
            report,
            "calculation.energy_present",
            "存在最终能量",
            CalculationValidationScope.Numerical,
            "存在",
            GetEnergyText(request.Result),
            request.Result.Energy.HasValue,
            "结果中没有最终能量。");

        AddRequiredCheck(
            report,
            "calculation.energy_finite",
            "最终能量是有限数值",
            CalculationValidationScope.Numerical,
            "有限数值",
            GetEnergyText(request.Result),
            request.Result.Energy.HasValue && double.IsFinite(request.Result.Energy.Value),
            "最终能量不是有限数值。");

        AddRequiredCheck(
            report,
            "calculation.energy_unit",
            "能量单位为 Hartree",
            CalculationValidationScope.Numerical,
            "Hartree",
            request.Result.EnergyUnit,
            string.Equals(
                request.Result.EnergyUnit,
                "Hartree",
                StringComparison.OrdinalIgnoreCase),
            "能量单位不是 Hartree。");

        AddRequiredCheck(
            report,
            "calculation.output_file",
            "输出文件存在",
            CalculationValidationScope.ProgramOutput,
            "存在",
            request.Result.OutputFilePath,
            !string.IsNullOrWhiteSpace(request.Result.OutputFilePath)
                && File.Exists(request.Result.OutputFilePath),
            "结果对应的输出文件不存在。");

        SetReportStatus(report);
        return report;
    }

    private static void AddRequiredCheck(
        CalculationValidationReport report,
        string code,
        string description,
        CalculationValidationScope scope,
        string expectedValue,
        string actualValue,
        bool passed,
        string failureMessage)
    {
        CalculationValidationCheck check = new CalculationValidationCheck();
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

            CalculationValidationIssue issue = new CalculationValidationIssue();
            issue.Severity = CalculationDiagnosticSeverity.Error;
            issue.Code = code;
            issue.Message = failureMessage;
            report.Issues.Add(issue);
        }

        report.Checks.Add(check);
    }

    private static void SetReportStatus(CalculationValidationReport report)
    {
        bool hasError = false;
        bool hasWarning = false;

        for (int index = 0; index < report.Issues.Count; index++)
        {
            if (report.Issues[index].Severity == CalculationDiagnosticSeverity.Error)
            {
                hasError = true;
            }
            else if (report.Issues[index].Severity == CalculationDiagnosticSeverity.Warning)
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
            report.Status = CalculationValidationStatus.PassedWithWarnings;
        }
        else
        {
            report.Status = CalculationValidationStatus.Passed;
        }
    }

    private static string GetEnergyText(CalculationResult result)
    {
        if (!result.Energy.HasValue)
        {
            return "缺失";
        }

        return result.Energy.Value.ToString(
            "G17",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GetScfConvergenceText(
        CalculationResult result)
    {
        if (!result.ScfConverged.HasValue)
        {
            return "未知";
        }

        return result.ScfConverged.Value
            ? "已收敛"
            : "未收敛";
    }
}
