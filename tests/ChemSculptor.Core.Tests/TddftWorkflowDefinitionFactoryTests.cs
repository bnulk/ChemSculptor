using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>TDDFT 单点工作流定义测试。</summary>
public class TddftWorkflowDefinitionFactoryTests
{
    /// <summary>验证工作流包含 TDDFT 单点计算节点。</summary>
    [Fact]
    public void ContainsTddftSinglePointNode()
    {
        WorkflowDefinition definition =
            TddftWorkflowDefinitionFactory.Create(
                "workflow-tddft",
                "TDDFT 单点计算");

        Assert.Single(definition.Nodes);
        WorkflowNode node = definition.Nodes[0];
        Assert.Equal("tddft-single-point", node.Id);
        Assert.Equal(
            CalculationSkillIds.CalculationTddftSinglePoint,
            node.Skill);
        Assert.Contains(
            "$input.request",
            node.Inputs.Values);
    }
}
