using ChemSculptor.Compute;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>验证联合优化频率作业中的振动分析结果。</summary>
public sealed class FrequencyAnalysisResultValidator
    : ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    public string Name
    {
        get { return "frequency-analysis-result-validator"; }
    }

    /// <summary>只处理包含振动结果的优化频率任务。</summary>
    public bool CanValidate(
        CalculationResultValidationRequest request)
    {
        if (request == null)
        {
            return false;
        }

        return request.Job.Spec.TaskType
                == CalculationTaskType.OptimizationFrequency
            && request.FrequencyResult != null;
    }

    /// <summary>验证振动分析结果。</summary>
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

        FrequencyAnalysisResult result =
            request.FrequencyResult!;

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "frequency.job_id",
            "振动结果作业标识与作业一致",
            CalculationValidationScope.Structure,
            request.Job.JobId,
            result.JobId,
            string.Equals(
                request.Job.JobId,
                result.JobId,
                StringComparison.OrdinalIgnoreCase),
            "振动结果中的作业标识与请求不一致。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "frequency.normal_termination",
            "振动分析正常结束",
            CalculationValidationScope.ProgramOutput,
            "true",
            result.NormalTermination.ToString(),
            result.NormalTermination,
            "振动分析没有正常结束。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "frequency.frequencies_present",
            "解析到振动频率",
            CalculationValidationScope.Numerical,
            "至少一个频率",
            result.Frequencies.Count.ToString(),
            result.Frequencies.Count > 0,
            "振动分析输出中没有解析到频率。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "frequency.minimum_present",
            "存在最低频率",
            CalculationValidationScope.Numerical,
            "存在",
            result.MinimumFrequency.HasValue
                ? result.MinimumFrequency.Value.ToString(
                    "G10",
                    System.Globalization.CultureInfo.InvariantCulture)
                : "缺失",
            result.MinimumFrequency.HasValue,
            "振动分析结果中没有最低频率。");

        NumericalCalculationValidationChecks.AddRecommendedCheck(
            report,
            "frequency.no_imaginary_modes",
            "没有虚频",
            CalculationValidationScope.ScientificPlausibility,
            "0",
            result.ImaginaryFrequencyCount.ToString(),
            result.ImaginaryFrequencyCount == 0,
            "振动分析发现虚频，当前结构不是稳定最小点。");

        NumericalCalculationValidationChecks.SetReportStatus(
            report);
        return report;
    }
}
