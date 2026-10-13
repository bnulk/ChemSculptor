using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationCalculation;

/// <summary>验证一次几何优化作业的正常结果。</summary>
public sealed class OptimizationCalculationValidationSkill : ISkill
{
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建优化结果验证 Skill。</summary>
    public OptimizationCalculationValidationSkill(
        ICalculationRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds.CalculationOptimizationValidation);
        _capabilities.Add("calculation.validation");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationOptimizationValidation;
        }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>验证优化结果并更新作业状态。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? waitJson;
        string? extractionJson;

        if (!request.Inputs.TryGetValue("wait", out waitJson))
        {
            throw new InvalidOperationException(
                "优化验证节点缺少 wait 输入。");
        }

        if (!request.Inputs.TryGetValue(
            "extraction",
            out extractionJson))
        {
            throw new InvalidOperationException(
                "优化验证节点缺少 extraction 输入。");
        }

        if (string.IsNullOrWhiteSpace(waitJson)
            || string.IsNullOrWhiteSpace(extractionJson))
        {
            throw new InvalidOperationException(
                "优化验证节点输入不能为空。");
        }

        CalculationWaitSkillResult waitResult =
            SkillJson.Deserialize<CalculationWaitSkillResult>(
                waitJson);
        CalculationResultExtractionResult extractionResult =
            SkillJson.Deserialize<
                CalculationResultExtractionResult>(
                    extractionJson);

        if (extractionResult.OptimizationResult == null)
        {
            throw new InvalidOperationException(
                "结果提取中没有 OptimizationResult。");
        }

        OptimizationResult optimizationResult =
            extractionResult.OptimizationResult;
        CalculationValidationReport report =
            BuildReport(
                waitResult.Job,
                optimizationResult);
        CalculationJob job = waitResult.Job;

        if (waitResult.State == CalculationJobState.Completed
            && report.Passed)
        {
            job.State = CalculationJobState.Validated;
        }
        else
        {
            job.State = CalculationJobState.Failed;
        }

        await _repository.SaveValidationAsync(
            report,
            job.JobId,
            cancellationToken);
        await _repository.SaveJobAsync(job, cancellationToken);

        OptimizationValidationSkillResult result =
            new OptimizationValidationSkillResult();
        result.Passed = report.Passed;
        result.Job = job;
        result.Result = optimizationResult;
        result.Report = report;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return taskResult;
    }

    /// <summary>检查技能可用性。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static CalculationValidationReport BuildReport(
        CalculationJob job,
        OptimizationResult result)
    {
        CalculationValidationReport report =
            new CalculationValidationReport();
        report.ValidatorName = "optimization-result-validator";
        report.ValidatedAt = DateTimeOffset.UtcNow;

        AddRequiredCheck(
            report,
            "optimization.job_id",
            "优化结果作业标识与作业一致",
            CalculationValidationScope.Structure,
            job.JobId,
            result.JobId,
            string.Equals(
                job.JobId,
                result.JobId,
                StringComparison.OrdinalIgnoreCase),
            "优化结果中的作业标识与请求不一致。");

        AddRequiredCheck(
            report,
            "optimization.program",
            "计算程序与方案一致",
            CalculationValidationScope.Structure,
            job.Spec.Program,
            result.Program,
            string.Equals(
                job.Spec.Program,
                result.Program,
                StringComparison.OrdinalIgnoreCase),
            "优化结果中的计算程序与方案不一致。");

        AddRequiredCheck(
            report,
            "optimization.normal_termination",
            "优化正常结束",
            CalculationValidationScope.ProgramOutput,
            "true",
            result.NormalTermination.ToString(),
            result.NormalTermination,
            "优化没有正常结束。");

        AddRequiredCheck(
            report,
            "optimization.completed",
            "几何优化已经完成",
            CalculationValidationScope.ScientificPlausibility,
            "true",
            GetBooleanText(result.OptimizationConverged),
            result.OptimizationConverged == true,
            "几何优化没有完成或收敛状态未知。");

        AddRequiredCheck(
            report,
            "optimization.failure_kind",
            "优化失败类别为 None",
            CalculationValidationScope.Structure,
            OptimizationFailureKind.None.ToString(),
            result.FailureKind.ToString(),
            result.FailureKind == OptimizationFailureKind.None,
            "优化被标记为失败：" + result.FailureKind.ToString());

        AddRequiredCheck(
            report,
            "optimization.energy_present",
            "存在最终能量",
            CalculationValidationScope.Numerical,
            "有限数值",
            GetEnergyText(result),
            result.FinalEnergy.HasValue
                && double.IsFinite(result.FinalEnergy.Value),
            "优化结果中没有有限最终能量。");

        AddRequiredCheck(
            report,
            "optimization.trajectory_present",
            "保留完整优化轨迹",
            CalculationValidationScope.Structure,
            "至少一个步骤",
            result.Trajectory.Count.ToString(),
            result.Trajectory.Count > 0
                && result.StepCount == result.Trajectory.Count,
            "优化轨迹为空或步骤数量与轨迹长度不一致。");

        AddRequiredCheck(
            report,
            "optimization.final_convergence",
            "最终收敛指标全部满足",
            CalculationValidationScope.Numerical,
            "true",
            result.FinalConvergence == null
                ? "缺失"
                : GetBooleanText(
                    result.FinalConvergence.Converged),
            result.FinalConvergence != null
                && result.FinalConvergence.Converged == true,
            "优化最终收敛指标缺失或未全部满足。");

        AddRequiredCheck(
            report,
            "optimization.final_geometry",
            "存在最终几何结构",
            CalculationValidationScope.Structure,
            "至少一个原子",
            result.FinalGeometry.Atoms.Count.ToString(),
            result.FinalGeometry.Atoms.Count > 0,
            "优化结果中没有最终几何结构。");

        AddRequiredCheck(
            report,
            "optimization.output_file",
            "输出文件存在",
            CalculationValidationScope.ProgramOutput,
            "存在",
            result.OutputFilePath,
            !string.IsNullOrWhiteSpace(result.OutputFilePath)
                && File.Exists(result.OutputFilePath),
            "优化结果对应的输出文件不存在。");

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

    private static void SetReportStatus(
        CalculationValidationReport report)
    {
        bool hasError = false;
        bool hasWarning = false;

        for (int index = 0;
            index < report.Issues.Count;
            index++)
        {
            if (report.Issues[index].Severity
                == CalculationDiagnosticSeverity.Error)
            {
                hasError = true;
            }
            else if (report.Issues[index].Severity
                == CalculationDiagnosticSeverity.Warning)
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

    private static string GetEnergyText(
        OptimizationResult result)
    {
        if (!result.FinalEnergy.HasValue)
        {
            return "缺失";
        }

        return result.FinalEnergy.Value.ToString(
            "G17",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string GetBooleanText(bool? value)
    {
        if (!value.HasValue)
        {
            return "未知";
        }

        return value.Value ? "true" : "false";
    }
}
