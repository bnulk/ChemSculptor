using ChemSculptor.Anomaly.Models;
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
            AnomalySkillIds.CheckOptimizationStability;
        stabilityCheck.DependsOn.Add(node.Id);
        stabilityCheck.Inputs["optimizationFrequency"] =
            node.Id;
        definition.Nodes.Add(stabilityCheck);

        WorkflowNode correctionPlan = new WorkflowNode();
        correctionPlan.Id =
            "optimization-frequency-stability-correction-plan";
        correctionPlan.Skill =
            AnomalySkillIds.PlanWavefunctionStabilityCorrection;
        correctionPlan.DependsOn.Add(stabilityCheck.Id);
        correctionPlan.Inputs["stability"] = stabilityCheck.Id;
        correctionPlan.ConditionInput = stabilityCheck.Id;
        AddConditions(
            correctionPlan,
            "wavefunction-instability");
        definition.Nodes.Add(correctionPlan);

        WorkflowNode recoveryJob = new WorkflowNode();
        recoveryJob.Id =
            "optimization-frequency-stability-recovery-job";
        recoveryJob.Skill = AnomalySkillIds.CreateRecoveryJob;
        recoveryJob.DependsOn.Add(correctionPlan.Id);
        recoveryJob.Inputs["correctionPlan"] =
            correctionPlan.Id;
        AddPlanCreatedCondition(
            recoveryJob,
            correctionPlan.Id);
        definition.Nodes.Add(recoveryJob);

        WorkflowNode recoveryExecution = new WorkflowNode();
        recoveryExecution.Id =
            "optimization-frequency-stability-recovery-execution";
        recoveryExecution.Skill =
            AnomalySkillIds.ExecuteRecoveryJob;
        recoveryExecution.DependsOn.Add(recoveryJob.Id);
        recoveryExecution.Inputs["correctionPlan"] =
            correctionPlan.Id;
        recoveryExecution.Inputs["recoveryJob"] = recoveryJob.Id;
        AddPlanCreatedCondition(
            recoveryExecution,
            correctionPlan.Id);
        definition.Nodes.Add(recoveryExecution);

        WorkflowNode recoveryStabilityCheck =
            new WorkflowNode();
        recoveryStabilityCheck.Id =
            "optimization-frequency-stability-recheck";
        recoveryStabilityCheck.Skill =
            AnomalySkillIds.CheckOptimizationStability;
        recoveryStabilityCheck.DependsOn.Add(
            recoveryExecution.Id);
        recoveryStabilityCheck.Inputs["optimizationFrequency"] =
            node.Id;
        recoveryStabilityCheck.Inputs["recoveryExecution"] =
            recoveryExecution.Id;
        AddPlanCreatedCondition(
            recoveryStabilityCheck,
            correctionPlan.Id);
        definition.Nodes.Add(recoveryStabilityCheck);

        return definition;
    }

    private static void AddPlanCreatedCondition(
        WorkflowNode node,
        string conditionNodeId)
    {
        node.ConditionInput = conditionNodeId;
        AddConditions(
            node,
            "\"planCreated\":true",
            "\"PlanCreated\":true");
    }

    private static void AddConditions(
        WorkflowNode node,
        params string[] conditions)
    {
        for (int index = 0;
            index < conditions.Length;
            index++)
        {
            node.ConditionAnyContains.Add(
                conditions[index]);
        }
    }
}
