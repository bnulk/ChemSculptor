using ChemSculptor.Anomaly.Models;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>选择由 SCF 或优化修复分支生成的修正计划。</summary>
public sealed class OptimizationRecoveryBranchSelectionSkill
    : ISkill
{
    private const string ScfBranchKey = "scfBranch";
    private const string OptimizationBranchKey = "optimizationBranch";

    private readonly List<string> _capabilities;

    /// <summary>创建分支选择 Skill。</summary>
    public OptimizationRecoveryBranchSelectionSkill()
    {
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.SelectOptimizationRecoveryBranch);
        _capabilities.Add("anomaly.recovery-selection");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds
                .SelectOptimizationRecoveryBranch;
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

    /// <summary>选择非空分支结果。</summary>
    public Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string scfJson = ReadInput(
            request,
            ScfBranchKey);
        string optimizationJson = ReadInput(
            request,
            OptimizationBranchKey);
        string selectedJson = SelectJson(
            scfJson,
            optimizationJson);

        if (selectedJson.Length == 0)
        {
            OptimizationCorrectionPlanningResult empty =
                new OptimizationCorrectionPlanningResult();
            empty.PlanCreated = false;
            empty.Message = "SCF 和优化修复分支都没有生成计划。";
            selectedJson = SkillJson.Serialize(empty);
        }

        TaskResult result = new TaskResult();
        result.WorkflowId = request.WorkflowId;
        result.NodeId = request.NodeId;
        result.Succeeded = true;
        result.Output = selectedJson;
        return Task.FromResult(result);
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static string ReadInput(
        TaskRequest request,
        string key)
    {
        string? value;

        if (!request.Inputs.TryGetValue(key, out value)
            || string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value;
    }

    private static string SelectJson(
        string scfJson,
        string optimizationJson)
    {
        if (!string.IsNullOrWhiteSpace(scfJson))
        {
            return scfJson;
        }

        if (!string.IsNullOrWhiteSpace(optimizationJson))
        {
            return optimizationJson;
        }

        return string.Empty;
    }
}
