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
    private readonly List<string> _capabilities;

    /// <summary>创建验证技能。</summary>
    public CalculationResultValidationSkill()
    {
        _capabilities = new List<string>();
        _capabilities.Add("calculation.validation");
        _capabilities.Add("calculation.result");
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
        CalculationValidationReport report = new CalculationValidationReport();
        report.Passed = true;

        if (!request.Result.NormalTermination)
        {
            AddIssue(
                report,
                "calculation.normal_termination_missing",
                "计算没有正常结束。");
        }

        if (!request.Result.Energy.HasValue)
        {
            AddIssue(
                report,
                "calculation.energy_missing",
                "计算结果中没有最终能量。");
        }

        if (request.Result.FailureKind != CalculationFailureKind.None)
        {
            AddIssue(
                report,
                "calculation.failure_kind",
                "计算结果标记为失败：" + request.Result.FailureKind.ToString());
        }

        CalculationResultValidationSkillResult result =
            new CalculationResultValidationSkillResult();
        result.Passed = report.Passed;
        result.Report = report;
        return Task.FromResult(result);
    }

    /// <summary>验证技能始终可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static void AddIssue(
        CalculationValidationReport report,
        string code,
        string message)
    {
        CalculationValidationIssue issue = new CalculationValidationIssue();
        issue.Severity = CalculationDiagnosticSeverity.Error;
        issue.Code = code;
        issue.Message = message;
        report.Issues.Add(issue);
        report.Passed = false;
    }
}
