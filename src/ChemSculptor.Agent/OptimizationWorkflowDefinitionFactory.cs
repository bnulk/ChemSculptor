using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.Agent;

/// <summary>完整几何优化工作流工厂。</summary>
public static class OptimizationWorkflowDefinitionFactory
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

        WorkflowNode optimization = new WorkflowNode();
        optimization.Id = "optimization";
        optimization.Skill =
            CalculationSkillIds.CalculationOptimization;
        optimization.Inputs["request"] = "$input.request";
        definition.Nodes.Add(optimization);

        WorkflowNode validation = new WorkflowNode();
        validation.Id = "optimization-validation";
        validation.Skill =
            CalculationSkillIds.CalculationOptimizationValidation;
        validation.DependsOn.Add(optimization.Id);
        validation.Inputs["optimization"] = optimization.Id;
        definition.Nodes.Add(validation);

        WorkflowNode anomaly = new WorkflowNode();
        anomaly.Id = "optimization-anomaly";
        anomaly.Skill =
            AnomalySkillIds.CheckOptimizationConvergence;
        anomaly.DependsOn.Add(validation.Id);
        anomaly.Inputs["optimization"] = optimization.Id;
        definition.Nodes.Add(anomaly);

        WorkflowNode diagnosis = new WorkflowNode();
        diagnosis.Id = "optimization-diagnosis";
        diagnosis.Skill =
            AnomalySkillIds.DiagnoseOptimizationFailure;
        diagnosis.DependsOn.Add(anomaly.Id);
        diagnosis.Inputs["anomaly"] = anomaly.Id;
        definition.Nodes.Add(diagnosis);

        WorkflowNode scfBranch = new WorkflowNode();
        scfBranch.Id = "scf-repair-branch";
        scfBranch.Skill =
            AnomalySkillIds.PlanOptimizationCorrection;
        scfBranch.DependsOn.Add(diagnosis.Id);
        scfBranch.Inputs["diagnosis"] = diagnosis.Id;
        scfBranch.ConditionInput = anomaly.Id;
        AddConditions(
            scfBranch,
            CommonAnomalyCodes.ScfNotConverged,
            CommonAnomalyCodes.ScfMaxCycles,
            CommonAnomalyCodes.ScfGuessFailed);
        definition.Nodes.Add(scfBranch);

        WorkflowNode optimizationBranch = new WorkflowNode();
        optimizationBranch.Id = "optimization-repair-branch";
        optimizationBranch.Skill =
            AnomalySkillIds.PlanOptimizationCorrection;
        optimizationBranch.DependsOn.Add(diagnosis.Id);
        optimizationBranch.Inputs["diagnosis"] = diagnosis.Id;
        optimizationBranch.ConditionInput = anomaly.Id;
        AddConditions(
            optimizationBranch,
            OptimizationAnomalyCodes.NotConverged,
            OptimizationAnomalyCodes.MaxCyclesExceeded,
            OptimizationAnomalyCodes.ForceNotConverged,
            OptimizationAnomalyCodes.DisplacementNotConverged,
            OptimizationAnomalyCodes.Oscillation,
            OptimizationAnomalyCodes.StepRejected,
            OptimizationAnomalyCodes.TrustRadiusFailure,
            OptimizationAnomalyCodes.CheckpointMissing);
        definition.Nodes.Add(optimizationBranch);

        WorkflowNode branchSelection = new WorkflowNode();
        branchSelection.Id = "recovery-branch-selection";
        branchSelection.Skill =
            AnomalySkillIds.SelectOptimizationRecoveryBranch;
        branchSelection.DependsOn.Add(scfBranch.Id);
        branchSelection.DependsOn.Add(optimizationBranch.Id);
        branchSelection.Inputs["scfBranch"] = scfBranch.Id;
        branchSelection.Inputs["optimizationBranch"] =
            optimizationBranch.Id;
        AddConditions(
            branchSelection,
            CommonAnomalyCodes.ScfNotConverged,
            CommonAnomalyCodes.ScfMaxCycles,
            CommonAnomalyCodes.ScfGuessFailed,
            OptimizationAnomalyCodes.NotConverged,
            OptimizationAnomalyCodes.MaxCyclesExceeded,
            OptimizationAnomalyCodes.ForceNotConverged,
            OptimizationAnomalyCodes.DisplacementNotConverged,
            OptimizationAnomalyCodes.Oscillation,
            OptimizationAnomalyCodes.StepRejected,
            OptimizationAnomalyCodes.TrustRadiusFailure,
            OptimizationAnomalyCodes.CheckpointMissing);
        branchSelection.ConditionInput = anomaly.Id;
        definition.Nodes.Add(branchSelection);

        WorkflowNode recoveryJob = new WorkflowNode();
        recoveryJob.Id = "optimization-recovery-job";
        recoveryJob.Skill =
            AnomalySkillIds.CreateOptimizationRecoveryJob;
        recoveryJob.DependsOn.Add(branchSelection.Id);
        recoveryJob.Inputs["correctionPlan"] =
            branchSelection.Id;
        AddPlanCreatedCondition(recoveryJob, branchSelection.Id);
        definition.Nodes.Add(recoveryJob);

        WorkflowNode recoveryExecution = new WorkflowNode();
        recoveryExecution.Id = "optimization-recovery-execution";
        recoveryExecution.Skill =
            AnomalySkillIds.ExecuteOptimizationRecoveryJob;
        recoveryExecution.DependsOn.Add(recoveryJob.Id);
        recoveryExecution.Inputs["correctionPlan"] =
            branchSelection.Id;
        recoveryExecution.Inputs["recoveryJob"] =
            recoveryJob.Id;
        AddPlanCreatedCondition(
            recoveryExecution,
            branchSelection.Id);
        definition.Nodes.Add(recoveryExecution);

        WorkflowNode recoveryValidation = new WorkflowNode();
        recoveryValidation.Id = "optimization-recovery-validation";
        recoveryValidation.Skill =
            AnomalySkillIds.ValidateOptimizationRecovery;
        recoveryValidation.DependsOn.Add(recoveryExecution.Id);
        recoveryValidation.Inputs["originalAnomaly"] =
            anomaly.Id;
        recoveryValidation.Inputs["recoveryExecution"] =
            recoveryExecution.Id;
        AddPlanCreatedCondition(
            recoveryValidation,
            branchSelection.Id);
        definition.Nodes.Add(recoveryValidation);

        WorkflowNode hardFailure = new WorkflowNode();
        hardFailure.Id = "optimization-hard-failure";
        hardFailure.Skill =
            AnomalySkillIds.ReportOptimizationHardFailure;
        hardFailure.DependsOn.Add(anomaly.Id);
        hardFailure.Inputs["anomaly"] = anomaly.Id;
        hardFailure.ConditionInput = anomaly.Id;
        AddConditions(
            hardFailure,
            CommonAnomalyCodes.OutputMissing,
            CommonAnomalyCodes.NormalTerminationMissing);
        definition.Nodes.Add(hardFailure);

        WorkflowNode processingPlan = new WorkflowNode();
        processingPlan.Id = "optimization-processing-plan";
        processingPlan.Skill =
            CalculationSkillIds.CalculationOptimizationProcessingPlan;
        processingPlan.DependsOn.Add(recoveryValidation.Id);
        processingPlan.DependsOn.Add(hardFailure.Id);
        processingPlan.Inputs["optimization"] = optimization.Id;
        processingPlan.Inputs["recoveryExecution"] =
            recoveryExecution.Id;
        processingPlan.Inputs["recoveryValidation"] =
            recoveryValidation.Id;
        definition.Nodes.Add(processingPlan);

        WorkflowNode scienceData = new WorkflowNode();
        scienceData.Id = "optimization-science-data";
        scienceData.Skill =
            ScientificDataSkillIds.RecordOptimizationResult;
        scienceData.DependsOn.Add(processingPlan.Id);
        scienceData.Inputs["optimization"] = optimization.Id;
        scienceData.Inputs["processingPlan"] =
            processingPlan.Id;
        scienceData.Inputs["recoveryExecution"] =
            recoveryExecution.Id;
        scienceData.Inputs["recoveryValidation"] =
            recoveryValidation.Id;
        definition.Nodes.Add(scienceData);

        return definition;
    }

    private static void AddPlanCreatedCondition(
        WorkflowNode node,
        string planNodeId)
    {
        node.ConditionInput = planNodeId;
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
            node.ConditionAnyContains.Add(conditions[index]);
        }
    }
}
