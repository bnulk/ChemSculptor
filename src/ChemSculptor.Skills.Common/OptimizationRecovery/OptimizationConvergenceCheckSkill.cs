using ChemSculptor.Anomaly.Checks;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.OptimizationRecovery;

/// <summary>执行优化收敛异常检查。</summary>
public sealed class OptimizationConvergenceCheckSkill : ISkill
{
    private const string OptimizationKey = "optimization";

    private readonly OptimizationConvergenceCheck _check;
    private readonly List<string> _capabilities;

    /// <summary>创建优化收敛检查 Skill。</summary>
    public OptimizationConvergenceCheckSkill(
        OptimizationConvergenceCheck check)
    {
        if (check == null)
        {
            throw new ArgumentNullException(nameof(check));
        }

        _check = check;
        _capabilities = new List<string>();
        _capabilities.Add(
            AnomalySkillIds.CheckOptimizationConvergence);
        _capabilities.Add("anomaly.check");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return AnomalySkillIds.CheckOptimizationConvergence;
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

    /// <summary>执行检查。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? optimizationJson;

        if (!request.Inputs.TryGetValue(
            OptimizationKey,
            out optimizationJson)
            || string.IsNullOrWhiteSpace(optimizationJson))
        {
            throw new InvalidOperationException(
                "优化收敛检查节点缺少 optimization 输入。");
        }

        OptimizationCalculationSkillResult optimization =
            SkillJson.Deserialize<
                OptimizationCalculationSkillResult>(
                    optimizationJson);
        AnomalyContext context = new AnomalyContext();
        context.Job = optimization.Job;
        context.OptimizationResult = optimization.Result;
        AnomalyCheckResult checkResult =
            await _check.CheckAsync(
                context,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(checkResult);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
