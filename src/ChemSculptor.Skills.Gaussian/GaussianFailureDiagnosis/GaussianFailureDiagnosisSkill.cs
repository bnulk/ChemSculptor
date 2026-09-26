using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Gaussian.GaussianFailureDiagnosis;

/// <summary>
/// Gaussian 异常诊断技能框架。
/// 当前阶段只保留技能契约和注册位置，不处理异常。
/// </summary>
public sealed class GaussianFailureDiagnosisSkill : ISkill
{
    /// <summary>技能标识。</summary>
    public const string NameValue = CalculationSkillIds.GaussianFailureDiagnosis;

    private readonly List<string> _capabilities;

    /// <summary>创建异常诊断技能框架。</summary>
    public GaussianFailureDiagnosisSkill()
    {
        _capabilities = new List<string>();
        _capabilities.Add("calculation.failure-diagnosis");
        _capabilities.Add("gaussian.failure");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return NameValue; }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "0.1.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>当前阶段不执行异常诊断。</summary>
    public Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        TaskResult result = new TaskResult();
        result.WorkflowId = request.WorkflowId;
        result.NodeId = request.NodeId;
        result.Succeeded = false;
        result.Diagnostics = "Gaussian 异常诊断框架尚未实现。";
        return Task.FromResult(result);
    }

    /// <summary>框架阶段报告未就绪。</summary>
    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
