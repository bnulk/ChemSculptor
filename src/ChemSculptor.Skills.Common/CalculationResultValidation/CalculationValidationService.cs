using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>
/// 通用结果验证服务。
/// 合并全部适用验证器并计算最终通过状态。
/// </summary>
public sealed class CalculationValidationService
{
    private readonly List<ICalculationResultValidator> _validators;

    /// <summary>创建验证服务。</summary>
    public CalculationValidationService(
        IEnumerable<ICalculationResultValidator> validators)
    {
        if (validators == null)
        {
            throw new ArgumentNullException(nameof(validators));
        }

        _validators = new List<ICalculationResultValidator>(validators);

        if (_validators.Count == 0)
        {
            throw new ArgumentException(
                "至少需要注册一个计算结果验证器。",
                nameof(validators));
        }
    }

    /// <summary>执行验证并合并报告。</summary>
    public CalculationResultValidationResult Validate(
        CalculationResultValidationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        CalculationValidationReport combinedReport =
            new CalculationValidationReport();
        combinedReport.ValidatedAt = DateTimeOffset.UtcNow;

        bool validatorFound = false;

        for (int index = 0; index < _validators.Count; index++)
        {
            ICalculationResultValidator validator = _validators[index];

            if (!validator.CanValidate(request))
            {
                continue;
            }

            validatorFound = true;
            CalculationValidationReport validatorReport =
                validator.Validate(request);
            MergeReport(combinedReport, validatorReport);
        }

        if (!validatorFound)
        {
            CalculationValidationIssue issue = new CalculationValidationIssue();
            issue.Severity = CalculationDiagnosticSeverity.Error;
            issue.Code = "calculation.validator_not_found";
            issue.Message = "没有可处理该任务类型的计算结果验证器。";
            combinedReport.Issues.Add(issue);
        }

        SetCombinedReportStatus(combinedReport);

        CalculationResultValidationResult result =
            new CalculationResultValidationResult();
        result.Passed = combinedReport.Passed;
        result.Report = combinedReport;
        return result;
    }

    private static void MergeReport(
        CalculationValidationReport combinedReport,
        CalculationValidationReport validatorReport)
    {
        for (int index = 0; index < validatorReport.Checks.Count; index++)
        {
            combinedReport.Checks.Add(validatorReport.Checks[index]);
        }

        for (int index = 0; index < validatorReport.Issues.Count; index++)
        {
            combinedReport.Issues.Add(validatorReport.Issues[index]);
        }

        if (combinedReport.ValidatorName.Length == 0)
        {
            combinedReport.ValidatorName = validatorReport.ValidatorName;
        }
        else if (validatorReport.ValidatorName.Length > 0)
        {
            combinedReport.ValidatorName =
                combinedReport.ValidatorName + ", " + validatorReport.ValidatorName;
        }
    }

    private static void SetCombinedReportStatus(CalculationValidationReport report)
    {
        bool hasRequiredFailure = false;
        bool hasRecommendedFailure = false;
        bool hasInformationalFailure = false;
        int requiredCheckCount = 0;
        int passedRequiredCheckCount = 0;

        for (int index = 0; index < report.Checks.Count; index++)
        {
            CalculationValidationCheck check = report.Checks[index];

            if (check.Requirement == CalculationValidationRequirement.Required)
            {
                requiredCheckCount++;

                if (check.Passed)
                {
                    passedRequiredCheckCount++;
                }
                else
                {
                    hasRequiredFailure = true;
                }
            }
            else if (check.Requirement == CalculationValidationRequirement.Recommended
                && !check.Passed)
            {
                hasRecommendedFailure = true;
            }
            else if (check.Requirement == CalculationValidationRequirement.Informational
                && !check.Passed)
            {
                hasInformationalFailure = true;
            }
        }

        for (int index = 0; index < report.Issues.Count; index++)
        {
            CalculationDiagnosticSeverity severity = report.Issues[index].Severity;

            if (severity == CalculationDiagnosticSeverity.Error)
            {
                hasRequiredFailure = true;
            }
            else if (severity == CalculationDiagnosticSeverity.Warning)
            {
                hasRecommendedFailure = true;
            }
        }

        if (hasRequiredFailure)
        {
            report.Passed = false;
            report.Status = CalculationValidationStatus.Failed;
            report.Summary =
                "必要验证存在失败项。正常终结是必要条件，但不是充分条件。";
            return;
        }

        report.Passed = true;

        if (hasRecommendedFailure || hasInformationalFailure)
        {
            report.Status = CalculationValidationStatus.PassedWithWarnings;
            report.Summary =
                "全部必要验证通过，但存在建议项或信息项的失败结果。";
        }
        else
        {
            report.Status = CalculationValidationStatus.Passed;
            report.Summary =
                "全部必要验证通过。正常终结是必要条件，但不是充分条件。";
        }

        report.Summary = report.Summary +
            " 必要检查 " +
            passedRequiredCheckCount.ToString() +
            "/" +
            requiredCheckCount.ToString() +
            " 项通过。";
    }
}
