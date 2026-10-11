using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.Agent;

/// <summary>单点计算声明式工作流工厂。</summary>
public static class SinglePointWorkflowDefinitionFactory
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

        WorkflowNode singlePoint = new WorkflowNode();
        singlePoint.Id = "single-point";
        singlePoint.Skill = CalculationSkillIds.CalculationSinglePoint;
        singlePoint.Inputs["request"] = "$input.request";
        definition.Nodes.Add(singlePoint);

        WorkflowNode stabilityCheck = new WorkflowNode();
        stabilityCheck.Id = "stability-check";
        stabilityCheck.Skill = AnomalySkillIds.CheckWavefunctionStability;
        stabilityCheck.DependsOn.Add(singlePoint.Id);
        stabilityCheck.Inputs["validation"] = singlePoint.Id;
        definition.Nodes.Add(stabilityCheck);

        WorkflowNode stabilityCorrectionPlan = new WorkflowNode();
        stabilityCorrectionPlan.Id = "stability-correction-plan";
        stabilityCorrectionPlan.Skill =
            AnomalySkillIds.PlanWavefunctionStabilityCorrection;
        stabilityCorrectionPlan.DependsOn.Add(stabilityCheck.Id);
        stabilityCorrectionPlan.Inputs["stability"] = stabilityCheck.Id;
        definition.Nodes.Add(stabilityCorrectionPlan);

        WorkflowNode recoveryJob = new WorkflowNode();
        recoveryJob.Id = "recovery-job";
        recoveryJob.Skill = AnomalySkillIds.CreateRecoveryJob;
        recoveryJob.DependsOn.Add(stabilityCorrectionPlan.Id);
        recoveryJob.Inputs["correctionPlan"] =
            stabilityCorrectionPlan.Id;
        definition.Nodes.Add(recoveryJob);

        WorkflowNode recoveryExecution = new WorkflowNode();
        recoveryExecution.Id = "recovery-execution";
        recoveryExecution.Skill = AnomalySkillIds.ExecuteRecoveryJob;
        recoveryExecution.DependsOn.Add(recoveryJob.Id);
        recoveryExecution.Inputs["correctionPlan"] =
            stabilityCorrectionPlan.Id;
        recoveryExecution.Inputs["recoveryJob"] =
            recoveryJob.Id;
        definition.Nodes.Add(recoveryExecution);

        WorkflowNode recoveryStabilityCheck = new WorkflowNode();
        recoveryStabilityCheck.Id = "recovery-stability-check";
        recoveryStabilityCheck.Skill =
            AnomalySkillIds.CheckWavefunctionStability;
        recoveryStabilityCheck.DependsOn.Add(
            recoveryExecution.Id);
        recoveryStabilityCheck.Inputs["recoveryExecution"] =
            recoveryExecution.Id;
        definition.Nodes.Add(recoveryStabilityCheck);

        WorkflowNode plan = new WorkflowNode();
        plan.Id = "plan";
        plan.Skill = CalculationSkillIds.CalculationWorkflowProcessingPlan;
        plan.DependsOn.Add(recoveryStabilityCheck.Id);
        plan.Inputs["validation"] = singlePoint.Id;
        plan.Inputs["extraction"] = singlePoint.Id;
        definition.Nodes.Add(plan);

        WorkflowNode scientificData = new WorkflowNode();
        scientificData.Id = "science-data-record";
        scientificData.Skill =
            ScientificDataSkillIds.RecordCalculationResult;
        scientificData.DependsOn.Add(plan.Id);
        scientificData.Inputs["request"] = "$input.request";
        scientificData.Inputs["validation"] = singlePoint.Id;
        scientificData.Inputs["extraction"] = singlePoint.Id;
        scientificData.Inputs["stability"] = stabilityCheck.Id;
        scientificData.Inputs["correctionPlan"] =
            stabilityCorrectionPlan.Id;
        scientificData.Inputs["recoveryExecution"] =
            recoveryExecution.Id;
        scientificData.Inputs["recoveryStability"] =
            recoveryStabilityCheck.Id;
        definition.Nodes.Add(scientificData);

        return definition;
    }
}
