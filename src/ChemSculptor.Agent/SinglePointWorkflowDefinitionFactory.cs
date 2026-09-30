using ChemSculptor.Compute;
using ChemSculptor.Domain;

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

        WorkflowNode inputGeneration = new WorkflowNode();
        inputGeneration.Id = "input-generation";
        inputGeneration.Skill = CalculationSkillIds.CalculationInputPreparation;
        inputGeneration.Inputs["request"] = "$input.request";
        definition.Nodes.Add(inputGeneration);

        WorkflowNode submit = new WorkflowNode();
        submit.Id = "submit";
        submit.Skill = CalculationSkillIds.CalculationSubmission;
        submit.DependsOn.Add(inputGeneration.Id);
        submit.Inputs["inputResult"] = inputGeneration.Id;
        definition.Nodes.Add(submit);

        WorkflowNode wait = new WorkflowNode();
        wait.Id = "wait";
        wait.Skill = CalculationSkillIds.CalculationWait;
        wait.DependsOn.Add(submit.Id);
        wait.Inputs["submission"] = submit.Id;
        definition.Nodes.Add(wait);

        WorkflowNode extract = new WorkflowNode();
        extract.Id = "extract";
        extract.Skill = CalculationSkillIds.CalculationResultExtraction;
        extract.DependsOn.Add(wait.Id);
        extract.Inputs["submission"] = submit.Id;
        definition.Nodes.Add(extract);

        WorkflowNode validate = new WorkflowNode();
        validate.Id = "validate";
        validate.Skill = CalculationSkillIds.CalculationWorkflowValidation;
        validate.DependsOn.Add(extract.Id);
        validate.Inputs["wait"] = wait.Id;
        validate.Inputs["extraction"] = extract.Id;
        definition.Nodes.Add(validate);

        WorkflowNode plan = new WorkflowNode();
        plan.Id = "plan";
        plan.Skill = CalculationSkillIds.CalculationWorkflowProcessingPlan;
        plan.DependsOn.Add(validate.Id);
        plan.Inputs["validation"] = validate.Id;
        plan.Inputs["extraction"] = extract.Id;
        definition.Nodes.Add(plan);

        return definition;
    }
}
