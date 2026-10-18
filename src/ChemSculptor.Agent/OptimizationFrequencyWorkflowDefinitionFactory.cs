using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>联合优化频率工作流工厂。</summary>
public static class OptimizationFrequencyWorkflowDefinitionFactory
{
    /// <summary>创建工作流定义。</summary>
    public static WorkflowDefinition Create(
        string workflowId,
        string goal)
    {
        WorkflowDefinition definition =
            new WorkflowDefinition();
        definition.Id = workflowId;
        definition.Version = "1.0.0";
        definition.Goal = goal;
        definition.Nodes = new List<WorkflowNode>();

        WorkflowNode node = new WorkflowNode();
        node.Id = "optimization-frequency";
        node.Skill =
            CalculationSkillIds.CalculationOptimizationFrequency;
        node.Inputs["request"] = "$input.request";
        definition.Nodes.Add(node);

        WorkflowNode stabilityCheck =
            new WorkflowNode();
        stabilityCheck.Id =
            "optimization-frequency-stability-check";
        stabilityCheck.Skill =
            ChemSculptor.Anomaly.Models.AnomalySkillIds
                .CheckOptimizationStability;
        stabilityCheck.DependsOn.Add(node.Id);
        stabilityCheck.Inputs["optimizationFrequency"] =
            node.Id;
        definition.Nodes.Add(stabilityCheck);

        return definition;
    }
}
