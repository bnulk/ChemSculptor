using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>
/// 通用计算结果验证技能。
/// 当前验证正常结束、最终能量和通用失败类别。
/// </summary>
public sealed class CalculationResultValidationSkill
    : JsonSkill<
        CalculationResultValidationSkillRequest,
        CalculationResultValidationSkillResult>
{
    private readonly List<ICalculationResultValidator> _validators;
    private readonly List<string> _capabilities;

    /// <summary>创建验证技能。</summary>
    public CalculationResultValidationSkill(
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

        _capabilities = new List<string>();
        _capabilities.Add("calculation.validation");
        _capabilities.Add("calculation.result");
        _capabilities.Add("calculation.single-point-validation");
    }

    /// <summary>技能标识。</summary>
    public override string Name
    {
        get { return CalculationResultValidationSkillDescriptor.Id; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return CalculationResultValidationSkillDescriptor.VersionValue; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>执行验证。</summary>
    protected override Task<CalculationResultValidationSkillResult> ExecuteAsync(
        CalculationResultValidationSkillRequest request,
        CancellationToken cancellationToken)
    {
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

        CalculationResultValidationSkillResult result =
            new CalculationResultValidationSkillResult();
        result.Passed = combinedReport.Passed;
        result.Report = combinedReport;
        return Task.FromResult(result);
    }

    /// <summary>验证技能始终可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
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
