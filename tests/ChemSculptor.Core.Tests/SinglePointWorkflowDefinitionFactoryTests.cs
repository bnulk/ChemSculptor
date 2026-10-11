using ChemSculptor.Agent;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Models;

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

        WorkflowNode? singlePoint =
            FindNode(definition, "single-point");
        WorkflowNode? stabilityCheck =
            FindNode(definition, "stability-check");
        WorkflowNode? correctionPlan =
            FindNode(definition, "stability-correction-plan");
        WorkflowNode? recoveryJob =
            FindNode(definition, "recovery-job");
        WorkflowNode? recoveryExecution =
            FindNode(definition, "recovery-execution");
        WorkflowNode? recoveryStabilityCheck =
            FindNode(definition, "recovery-stability-check");
        WorkflowNode? plan = FindNode(definition, "plan");
        WorkflowNode? scientificData =
            FindNode(definition, "science-data-record");

        Assert.NotNull(singlePoint);
        Assert.NotNull(stabilityCheck);
        Assert.NotNull(correctionPlan);
        Assert.NotNull(recoveryJob);
        Assert.NotNull(recoveryExecution);
        Assert.NotNull(recoveryStabilityCheck);
        Assert.NotNull(plan);
        Assert.NotNull(scientificData);
        Assert.Equal(
            CalculationSkillIds.CalculationSinglePoint,
            singlePoint.Skill);
        Assert.Contains("$input.request", singlePoint.Inputs.Values);
        Assert.Equal(
            AnomalySkillIds.CheckWavefunctionStability,
            stabilityCheck.Skill);
        Assert.Contains("single-point", stabilityCheck.DependsOn);
        Assert.Equal(
            AnomalySkillIds.PlanWavefunctionStabilityCorrection,
            correctionPlan.Skill);
        Assert.Contains("stability-check", correctionPlan.DependsOn);
        Assert.Equal(
            AnomalySkillIds.CreateRecoveryJob,
            recoveryJob.Skill);
        Assert.Contains(
            "stability-correction-plan",
            recoveryJob.DependsOn);
        Assert.Equal(
            AnomalySkillIds.ExecuteRecoveryJob,
            recoveryExecution.Skill);
        Assert.Contains(
            "recovery-job",
            recoveryExecution.DependsOn);
        Assert.Equal(
            AnomalySkillIds.CheckWavefunctionStability,
            recoveryStabilityCheck.Skill);
        Assert.Contains(
            "recovery-execution",
            recoveryStabilityCheck.DependsOn);
        Assert.Contains(
            "recovery-stability-check",
            plan.DependsOn);
        Assert.Contains("single-point", plan.Inputs.Values);
        Assert.Equal(
            ScientificDataSkillIds.RecordCalculationResult,
            scientificData.Skill);
        Assert.Contains("plan", scientificData.DependsOn);
        Assert.Contains(
            "$input.request",
            scientificData.Inputs.Values);
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
