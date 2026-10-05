using System.Collections.Concurrent;
using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;

namespace ChemSculptor.Anomaly.Registry;

/// <summary>
/// 异常处理提供器的内存注册表。
/// 通过代码注册扩展，后续可以替换为从程序集或配置中自动发现。
/// </summary>
public sealed class AnomalyProviderRegistry : IAnomalyProviderRegistry
{
    private readonly ConcurrentDictionary<string, IAnomalyCheck> _checks =
        new ConcurrentDictionary<string, IAnomalyCheck>(
            StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, IAnomalyDiagnoser> _diagnosers =
        new ConcurrentDictionary<string, IAnomalyDiagnoser>(
            StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, ICorrectionPlanner> _correctionPlanners =
        new ConcurrentDictionary<string, ICorrectionPlanner>(
            StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, IApprovalPolicy> _approvalPolicies =
        new ConcurrentDictionary<string, IApprovalPolicy>(
            StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<string, IRecoveryValidator> _recoveryValidators =
        new ConcurrentDictionary<string, IRecoveryValidator>(
            StringComparer.OrdinalIgnoreCase);

    /// <summary>注册异常检查。</summary>
    public void RegisterCheck(IAnomalyCheck check)
    {
        if (check == null)
        {
            throw new ArgumentNullException(nameof(check));
        }

        string identity = GetCheckIdentity(check.Descriptor);

        if (!_checks.TryAdd(identity, check))
        {
            throw new InvalidOperationException(
                "异常检查已经注册：" + identity);
        }
    }

    /// <summary>注册异常诊断器。</summary>
    public void RegisterDiagnoser(IAnomalyDiagnoser diagnoser)
    {
        if (diagnoser == null)
        {
            throw new ArgumentNullException(nameof(diagnoser));
        }

        if (!_diagnosers.TryAdd(diagnoser.Code, diagnoser))
        {
            throw new InvalidOperationException(
                "异常诊断器已经注册：" + diagnoser.Code);
        }
    }

    /// <summary>注册修正规划器。</summary>
    public void RegisterCorrectionPlanner(ICorrectionPlanner planner)
    {
        if (planner == null)
        {
            throw new ArgumentNullException(nameof(planner));
        }

        if (!_correctionPlanners.TryAdd(planner.Code, planner))
        {
            throw new InvalidOperationException(
                "修正规划器已经注册：" + planner.Code);
        }
    }

    /// <summary>注册审批策略。</summary>
    public void RegisterApprovalPolicy(IApprovalPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        if (!_approvalPolicies.TryAdd(policy.Name, policy))
        {
            throw new InvalidOperationException(
                "审批策略已经注册：" + policy.Name);
        }
    }

    /// <summary>注册恢复验证器。</summary>
    public void RegisterRecoveryValidator(IRecoveryValidator validator)
    {
        if (validator == null)
        {
            throw new ArgumentNullException(nameof(validator));
        }

        if (!_recoveryValidators.TryAdd(validator.Code, validator))
        {
            throw new InvalidOperationException(
                "恢复验证器已经注册：" + validator.Code);
        }
    }

    /// <summary>列出全部异常检查。</summary>
    public IReadOnlyList<IAnomalyCheck> ListChecks()
    {
        return new List<IAnomalyCheck>(_checks.Values);
    }

    private static string GetCheckIdentity(
        AnomalyCheckDescriptor descriptor)
    {
        if (!string.IsNullOrWhiteSpace(descriptor.ImplementationId))
        {
            return descriptor.ImplementationId;
        }

        return descriptor.Code;
    }

    /// <summary>列出全部异常诊断器。</summary>
    public IReadOnlyList<IAnomalyDiagnoser> ListDiagnosers()
    {
        return new List<IAnomalyDiagnoser>(_diagnosers.Values);
    }

    /// <summary>列出全部修正规划器。</summary>
    public IReadOnlyList<ICorrectionPlanner> ListCorrectionPlanners()
    {
        return new List<ICorrectionPlanner>(_correctionPlanners.Values);
    }

    /// <summary>列出全部审批策略。</summary>
    public IReadOnlyList<IApprovalPolicy> ListApprovalPolicies()
    {
        return new List<IApprovalPolicy>(_approvalPolicies.Values);
    }

    /// <summary>列出全部恢复验证器。</summary>
    public IReadOnlyList<IRecoveryValidator> ListRecoveryValidators()
    {
        return new List<IRecoveryValidator>(_recoveryValidators.Values);
    }
}
