using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>单点计算工作流执行入口。</summary>
public interface ISinglePointWorkflowEngine
{
    /// <summary>提交工作流并在后台执行。</summary>
    Task<WorkflowRun> StartAsync(
        WorkflowDefinition definition,
        IReadOnlyDictionary<string, string> inputs,
        CalculationJob job,
        CancellationToken cancellationToken = default);
}
