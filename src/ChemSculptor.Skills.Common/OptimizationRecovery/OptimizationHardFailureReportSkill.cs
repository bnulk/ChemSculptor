using ChemSculptor.Anomaly.Models;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>报告无法自动恢复的优化程序硬失败。</summary>
public sealed class OptimizationHardFailureReportSkill : ISkill
{
    private const string AnomalyKey = "anomaly";

    private readonly List<string> _capabilities;

    /// <summary>创建硬失败报告 Skill。</summary>
    public OptimizationHardFailureReportSkill()
    {
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.ReportOptimizationHardFailure);
        _capabilities.Add("anomaly.report");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.ReportOptimizationHardFailure;
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

    /// <summary>生成失败报告。</summary>
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
                "优化硬失败节点缺少 anomaly 输入。");
        }

        AnomalyCheckResult anomaly =
            SkillJson.Deserialize<AnomalyCheckResult>(
                anomalyJson);
        string summary = anomaly.Summary.Length > 0
            ? anomaly.Summary
            : "优化程序出现无法自动恢复的硬失败。";

        TaskResult result = new TaskResult();
        result.WorkflowId = request.WorkflowId;
        result.NodeId = request.NodeId;
        result.Succeeded = false;
        result.Diagnostics = summary;
        return Task.FromResult(result);
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
