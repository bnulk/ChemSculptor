using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>TDDFT 单点计算声明式工作流工厂。</summary>
public static class TddftWorkflowDefinitionFactory
{
    /// <summary>创建工作流定义。</summary>
    public static WorkflowDefinition Create(
        string workflowId,
        string goal)
    {
        WorkflowDefinition definition = new WorkflowDefinition();
        definition.Id = workflowId;
        definition.Version = "1.0.0";
        definition.Goal = goal;
        definition.Nodes = new List<WorkflowNode>();

        WorkflowNode tddft = new WorkflowNode();
        tddft.Id = "tddft-single-point";
        tddft.Skill =
            CalculationSkillIds.CalculationTddftSinglePoint;
        tddft.Inputs["request"] = "$input.request";
        definition.Nodes.Add(tddft);

        return definition;
    }
}
