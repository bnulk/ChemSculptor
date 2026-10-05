using ChemSculptor.Agent;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>单点工作流定义测试。</summary>
public class SinglePointWorkflowDefinitionFactoryTests
{
    /// <summary>验证单点工作流始终包含波函数稳定性检查。</summary>
    [Fact]
    public void ContainsStabilityCheckBeforeProcessingPlan()
    {
        WorkflowDefinition definition =
            SinglePointWorkflowDefinitionFactory.Create(
                "workflow-single-point",
                "单点计算");

        WorkflowNode? stabilityCheck =
            FindNode(definition, "stability-check");
        WorkflowNode? plan = FindNode(definition, "plan");

        Assert.NotNull(stabilityCheck);
        Assert.NotNull(plan);
        Assert.Equal(
            AnomalySkillIds.CheckWavefunctionStability,
            stabilityCheck.Skill);
        Assert.Contains("validate", stabilityCheck.DependsOn);
        Assert.Contains("stability-check", plan.DependsOn);
    }

    private static WorkflowNode? FindNode(
        WorkflowDefinition definition,
        string nodeId)
    {
        for (int index = 0; index < definition.Nodes.Count; index++)
        {
            WorkflowNode node = definition.Nodes[index];

            if (string.Equals(
                node.Id,
                nodeId,
                StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }
        }

        return null;
    }
}
