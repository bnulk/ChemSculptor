using ChemSculptor.Anomaly.Models;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>把优化异常发现转换为原因诊断。</summary>
public sealed class OptimizationFailureDiagnosisSkill : ISkill
{
    private const string AnomalyKey = "anomaly";

    private readonly List<string> _capabilities;

    /// <summary>创建优化异常诊断 Skill。</summary>
    public OptimizationFailureDiagnosisSkill()
    {
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.DiagnoseOptimizationFailure);
        _capabilities.Add("anomaly.diagnosis");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.DiagnoseOptimizationFailure;
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

    /// <summary>诊断优化异常。</summary>
    public Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? anomalyJson;

        if (!request.Inputs.TryGetValue(
            AnomalyKey,
            out anomalyJson)
            || string.IsNullOrWhiteSpace(anomalyJson))
        {
            throw new InvalidOperationException(
                "优化诊断节点缺少 anomaly 输入。");
        }

        AnomalyCheckResult checkResult =
            SkillJson.Deserialize<AnomalyCheckResult>(
                anomalyJson);
        OptimizationFailureDiagnosisResult result =
            new OptimizationFailureDiagnosisResult();
        result.SourceJobId = checkResult.JobId;
        result.AnomalyRecordId = checkResult.AnomalyRecordId;

        for (int index = 0;
            index < checkResult.Findings.Count;
            index++)
        {
            result.Diagnoses.Add(
                Diagnose(checkResult.Findings[index]));
        }

        result.Summary = BuildSummary(result);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return Task.FromResult(taskResult);
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static DiagnosisReport Diagnose(
        AnomalyFinding finding)
    {
        DiagnosisReport report = new DiagnosisReport();
        report.AnomalyCode = finding.Code;
        report.Confidence = 0.9;
        report.Evidence = new List<AnomalyEvidence>(
            finding.Evidence);

        if (finding.Code == CommonAnomalyCodes.ScfNotConverged
            || finding.Code == CommonAnomalyCodes.ScfMaxCycles)
        {
            report.PrimaryCause = "SCF 未稳定收敛";
            report.Summary =
                "优化步骤中的 SCF 计算没有收敛，应先修复 SCF。";
            report.ContributingFactors.Add(
                "初始猜测质量不足");
            report.ContributingFactors.Add(
                "迭代上限不足");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.NotConverged)
        {
            report.PrimaryCause = "优化达到停止条件但仍未收敛";
            report.Summary =
                "优化过程结束，但最终力或位移指标未满足。";
            report.ContributingFactors.Add(
                "最大循环次数不足");
            report.ContributingFactors.Add(
                "初始 Hessian 质量不足");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.MaxCyclesExceeded)
        {
            report.PrimaryCause = "优化达到最大循环次数";
            report.Summary =
                "优化步数已经达到上限，需要增加循环次数或从检查点重启。";
            report.ContributingFactors.Add(
                "当前 opt=maxcycles 设置过低");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.ForceNotConverged)
        {
            report.PrimaryCause = "优化力指标未满足";
            report.Summary =
                "梯度尚未足够接近零，需要继续优化或改善 Hessian。";
            report.ContributingFactors.Add(
                "初始 Hessian 质量不足");
            report.ContributingFactors.Add(
                "优化算法步长不稳定");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.DisplacementNotConverged)
        {
            report.PrimaryCause = "优化位移指标未满足";
            report.Summary =
                "几何位移仍未达到收敛标准。";
            report.ContributingFactors.Add(
                "优化过程振荡");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.Oscillation)
        {
            report.PrimaryCause = "优化轨迹振荡";
            report.Summary =
                "优化步长和 Hessian 更新导致轨迹不能稳定收敛。";
            report.ContributingFactors.Add(
                "初始 Hessian 质量不足");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.StepRejected)
        {
            report.PrimaryCause = "优化步骤被拒绝";
            report.Summary =
                "优化器无法接受当前步长，需要调整 Hessian 或重启。";
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.TrustRadiusFailure)
        {
            report.PrimaryCause = "信赖半径更新失败";
            report.Summary =
                "优化信赖半径无法继续收敛。";
            report.ContributingFactors.Add(
                "几何或 Hessian 数值异常");
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.CheckpointMissing)
        {
            report.PrimaryCause = "优化检查点缺失";
            report.Summary =
                "无法从检查点继续优化，需要重新生成输入。";
            return report;
        }

        if (finding.Code
            == OptimizationAnomalyCodes.GeometryInvalid)
        {
            report.PrimaryCause = "优化几何无效";
            report.Summary =
                "最终结构缺失或无效，不能直接进入后续计算。";
            report.ContributingFactors.Add(
                "程序输出截断或几何优化失败");
            return report;
        }

        report.PrimaryCause = "未分类的优化异常";
        report.Summary = "需要人工检查优化输出和异常证据。";
        report.Confidence = 0.5;
        return report;
    }

    private static string BuildSummary(
        OptimizationFailureDiagnosisResult result)
    {
        if (result.Diagnoses.Count == 0)
        {
            return "没有需要诊断的优化异常。";
        }

        string summary =
            "已诊断 " +
            result.Diagnoses.Count.ToString() +
            " 项优化异常。";

        if (result.Diagnoses.Count > 0)
        {
            summary = summary +
                " 首要原因：" +
                result.Diagnoses[0].PrimaryCause +
                "。";
        }

        return summary;
    }
}
