using ChemSculptor.Anomaly.Abstractions;

namespace ChemSculptor.Anomaly.Registry;

/// <summary>异常处理提供器注册表。</summary>
public interface IAnomalyProviderRegistry
{
    /// <summary>注册异常检查。</summary>
    void RegisterCheck(IAnomalyCheck check);

    /// <summary>注册异常诊断器。</summary>
    void RegisterDiagnoser(IAnomalyDiagnoser diagnoser);

    /// <summary>注册修正规划器。</summary>
    void RegisterCorrectionPlanner(ICorrectionPlanner planner);

    /// <summary>注册审批策略。</summary>
    void RegisterApprovalPolicy(IApprovalPolicy policy);

    /// <summary>注册恢复验证器。</summary>
    void RegisterRecoveryValidator(IRecoveryValidator validator);

    /// <summary>注册派生恢复作业创建器。</summary>
    void RegisterRecoveryJobProvider(
        IRecoveryJobProvider provider);

    /// <summary>列出全部异常检查。</summary>
    IReadOnlyList<IAnomalyCheck> ListChecks();

    /// <summary>列出全部异常诊断器。</summary>
    IReadOnlyList<IAnomalyDiagnoser> ListDiagnosers();

    /// <summary>列出全部修正规划器。</summary>
    IReadOnlyList<ICorrectionPlanner> ListCorrectionPlanners();

    /// <summary>列出全部审批策略。</summary>
    IReadOnlyList<IApprovalPolicy> ListApprovalPolicies();

    /// <summary>列出全部恢复验证器。</summary>
    IReadOnlyList<IRecoveryValidator> ListRecoveryValidators();

    /// <summary>列出全部派生恢复作业创建器。</summary>
    IReadOnlyList<IRecoveryJobProvider> ListRecoveryJobProviders();

    /// <summary>按计算程序查找派生恢复作业创建器。</summary>
    IRecoveryJobProvider? ResolveRecoveryJobProvider(
        string program);
}
