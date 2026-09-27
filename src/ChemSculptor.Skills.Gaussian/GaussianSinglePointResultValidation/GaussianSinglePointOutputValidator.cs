using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Skills.Common.CalculationResultValidation;

namespace ChemSculptor.Skills.Gaussian.GaussianSinglePointResultValidation;

/// <summary>
/// Gaussian 单点输出文件验证器。
/// 检查输出文件的最后一个非空行是否包含 Normal termination。
/// </summary>
public sealed class GaussianSinglePointOutputValidator : ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    public string Name
    {
        get { return "gaussian-single-point-output-validator"; }
    }

    /// <summary>只处理 Gaussian 16 的单点计算。</summary>
    public bool CanValidate(CalculationResultValidationRequest request)
    {
        if (request.Job.Spec.TaskType != CalculationTaskType.SinglePoint)
        {
            return false;
        }

        return string.Equals(
            request.Job.Spec.Program,
            Gaussian16ProgramAdapter.ProgramNameValue,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>检查最后一行的正常终结标志。</summary>
    public CalculationValidationReport Validate(
        CalculationResultValidationRequest request)
    {
        CalculationValidationReport report = new CalculationValidationReport();
        report.ValidatorName = Name;
        report.ValidatedAt = DateTimeOffset.UtcNow;

        string lastNonEmptyLine = ReadLastNonEmptyLine(request.Result.OutputFilePath);
        bool passed =
            lastNonEmptyLine.IndexOf(
                "Normal termination",
                StringComparison.Ordinal) >= 0;

        CalculationValidationCheck check = new CalculationValidationCheck();
        check.Code = "gaussian.output_last_line_normal_termination";
        check.Description = "Gaussian 输出最后一个非空行包含 Normal termination";
        check.Passed = passed;
        check.Severity = CalculationDiagnosticSeverity.Error;
        check.Requirement = CalculationValidationRequirement.Required;
        check.Scope = CalculationValidationScope.ProgramOutput;
        check.ExpectedValue = "最后非空行包含 Normal termination";
        check.ActualValue = lastNonEmptyLine;

        if (passed)
        {
            check.Message = "通过。";
        }
        else
        {
            check.Message = "Gaussian 输出最后一个非空行没有 Normal termination。";

            CalculationValidationIssue issue = new CalculationValidationIssue();
            issue.Severity = CalculationDiagnosticSeverity.Error;
            issue.Code = check.Code;
            issue.Message = check.Message;
            report.Issues.Add(issue);
        }

        report.Checks.Add(check);
        SetReportStatus(report);
        return report;
    }

    private static string ReadLastNonEmptyLine(string outputFilePath)
    {
        if (string.IsNullOrWhiteSpace(outputFilePath)
            || !File.Exists(outputFilePath))
        {
            return string.Empty;
        }

        string[] lines = File.ReadAllLines(outputFilePath);

        for (int index = lines.Length - 1; index >= 0; index--)
        {
            string line = lines[index].Trim();
            if (line.Length > 0)
            {
                return line;
            }
        }

        return string.Empty;
    }

    private static void SetReportStatus(CalculationValidationReport report)
    {
        bool hasError = false;

        for (int index = 0; index < report.Issues.Count; index++)
        {
            if (report.Issues[index].Severity == CalculationDiagnosticSeverity.Error)
            {
                hasError = true;
                break;
            }
        }

        report.Passed = !hasError;

        if (hasError)
        {
            report.Status = CalculationValidationStatus.Failed;
        }
        else
        {
            report.Status = CalculationValidationStatus.Passed;
        }
    }
}
