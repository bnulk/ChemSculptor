using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;

namespace ChemSculptor.Skills.Common.AnomalyWorkflow;

/// <summary>
/// 通用波函数稳定性检查 Skill。
/// 根据计算程序选择具体实现，并统一返回 AnomalyCheckResult。
/// </summary>
public sealed class WavefunctionStabilityCheckSkill
    : JsonSkill<AnomalyCheckRequest, AnomalyCheckResult>
{
    private readonly IAnomalyProviderRegistry _registry;
    private readonly List<string> _capabilities;

    /// <summary>创建通用稳定性检查 Skill。</summary>
    public WavefunctionStabilityCheckSkill(
        IAnomalyProviderRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        _registry = registry;
        _capabilities = new List<string>();
        _capabilities.Add("anomaly.calculation-check");
        _capabilities.Add("anomaly.scientific-check");
    }

    /// <summary>技能名称。</summary>
    public override string Name
    {
        get { return AnomalySkillIds.CheckWavefunctionStability; }
    }

    /// <summary>技能版本。</summary>
    public override string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public override IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>选择具体实现并执行检查。</summary>
    protected override async Task<AnomalyCheckResult> ExecuteAsync(
        AnomalyCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CheckCode)
            && !string.Equals(
                request.CheckCode,
                CommonAnomalyCheckCodes.WavefunctionStability,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "通用稳定性检查 Skill 收到了不支持的检查代码：" +
                request.CheckCode);
        }

        List<IAnomalyCheck> candidates = new List<IAnomalyCheck>();
        IReadOnlyList<IAnomalyCheck> checks = _registry.ListChecks();

        for (int index = 0; index < checks.Count; index++)
        {
            IAnomalyCheck check = checks[index];

            if (!string.Equals(
                check.Descriptor.Code,
                CommonAnomalyCheckCodes.WavefunctionStability,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (check.CanCheck(request.Context))
            {
                candidates.Add(check);
            }
        }

        if (candidates.Count == 0)
        {
            return CreateUnavailableResult();
        }

        candidates.Sort(CompareChecks);
        return await candidates[0].CheckAsync(
            request.Context,
            cancellationToken);
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public override Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static int CompareChecks(
        IAnomalyCheck left,
        IAnomalyCheck right)
    {
        return string.Compare(
            left.Descriptor.ImplementationId,
            right.Descriptor.ImplementationId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static AnomalyCheckResult CreateUnavailableResult()
    {
        AnomalyCheckResult result = new AnomalyCheckResult();
        result.Code = CommonAnomalyCheckCodes.WavefunctionStability;
        result.DisplayName = "波函数稳定性检查";
        result.Category = AnomalyCategory.Scientific;
        result.Mechanism = AnomalyCheckMechanism.AuxiliaryCalculation;
        result.IsRequired = true;
        result.Status = AnomalyCheckStatus.Skipped;
        result.SkippedReason =
            "没有适用于当前任务和计算程序的波函数稳定性检查实现。";
        result.Summary = result.SkippedReason;
        result.StartedAt = DateTimeOffset.UtcNow;
        result.CompletedAt = DateTimeOffset.UtcNow;
        return result;
    }
}
