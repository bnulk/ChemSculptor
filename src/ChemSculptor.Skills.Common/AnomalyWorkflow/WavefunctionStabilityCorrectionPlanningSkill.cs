using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Planning;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.AnomalyWorkflow;

/// <summary>根据波函数稳定性检查结果生成通用修正方案。</summary>
public sealed class WavefunctionStabilityCorrectionPlanningSkill
    : ISkill
{
    private const string StabilityKey = "stability";

    private readonly WavefunctionStabilityCorrectionPlanner _planner;
    private readonly IAnomalyRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建修正方案规划 Skill。</summary>
    public WavefunctionStabilityCorrectionPlanningSkill(
        WavefunctionStabilityCorrectionPlanner planner,
        IAnomalyRepository repository)
    {
        if (planner == null)
        {
            throw new ArgumentNullException(nameof(planner));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _planner = planner;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("anomaly.correction-planning");
        _capabilities.Add("anomaly.wavefunction-stability");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get { return AnomalySkillIds.PlanWavefunctionStabilityCorrection; }
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

    /// <summary>根据稳定性检查结果生成并保存修正计划。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? stabilityJson;

        if (!request.Inputs.TryGetValue(
            StabilityKey,
            out stabilityJson)
            || string.IsNullOrWhiteSpace(stabilityJson))
        {
            throw new InvalidOperationException(
                "修正规划节点缺少 stability 输入。");
        }

        AnomalyCheckResult stabilityCheck =
            SkillJson.Deserialize<AnomalyCheckResult>(
                stabilityJson);
        WavefunctionStabilityCorrectionPlanningResult planningResult =
            await CreatePlanAsync(
                stabilityCheck,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(planningResult);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private async Task<WavefunctionStabilityCorrectionPlanningResult>
        CreatePlanAsync(
            AnomalyCheckResult stabilityCheck,
            CancellationToken cancellationToken)
    {
        WavefunctionStabilityCorrectionPlanningResult result =
            new WavefunctionStabilityCorrectionPlanningResult();

        if (stabilityCheck.Status != AnomalyCheckStatus.Finding
            || stabilityCheck.WavefunctionStability == null)
        {
            result.PlanCreated = false;
            result.Message = "当前稳定性检查没有产生修正需求。";
            return result;
        }

        CorrectionOption? option;
        string message;
        bool created = _planner.TryCreatePlan(
            stabilityCheck.WavefunctionStability,
            out option,
            out message);

        if (!created || option == null)
        {
            result.PlanCreated = false;
            result.Message = message;
            return result;
        }

        CorrectionPlan plan = new CorrectionPlan();
        plan.Id = "plan-" + Guid.NewGuid().ToString("N");
        plan.SourceJobId = stabilityCheck.JobId;
        plan.Option = option;
        result.PlanCreated = true;
        result.Plan = plan;
        result.Message = message;

        if (string.IsNullOrWhiteSpace(stabilityCheck.JobId)
            || string.IsNullOrWhiteSpace(stabilityCheck.AnomalyRecordId))
        {
            return result;
        }

        AnomalyRecord? record =
            await _repository.GetAsync(
                stabilityCheck.JobId,
                stabilityCheck.AnomalyRecordId,
                cancellationToken);

        if (record != null)
        {
            record.CorrectionPlans.Add(plan);
            record.Status = option.RequiresApproval
                ? AnomalyRecordStatus.AwaitingApproval
                : AnomalyRecordStatus.ReadyForRecovery;
            await _repository.SaveAsync(record, cancellationToken);
        }

        return result;
    }
}
