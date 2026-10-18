using ChemSculptor.Agent;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>联合优化频率 route 和任务解释测试。</summary>
public class OptimizationFrequencyRouteTests
{
    /// <summary>验证联合任务生成 opt 和 Freq，不生成 SP。</summary>
    [Fact]
    public void BuildsOptimizationFrequencyRoute()
    {
        CalculationSpec spec =
            CalculationDefaults
                .CreateDefaultOptimizationFrequency();
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationMaxCycles] = "300";
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationInitialHessian] =
            "CalcFC";

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuilder().Build(spec);

        Assert.Equal(
            "opt=(CalcFC, MaxCycles=300) Freq",
            result.TaskKeyword);
        Assert.Contains("opt=(", result.RouteLine);
        Assert.Contains("Freq", result.RouteLine);
        Assert.DoesNotContain("SP", result.RouteLine);
    }

    /// <summary>验证自然语言和 opt freq route 都会识别为联合任务。</summary>
    [Theory]
    [InlineData("优化并做振动分析")]
    [InlineData("优化 振动分析")]
    [InlineData("优化后振动分析")]
    [InlineData("opt freq")]
    [InlineData("#p CAM-B3LYP/6-31G* opt freq")]
    public async Task InterpretsOptimizationFrequencyTask(
        string text)
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask result =
            await interpreter.InterpretAsync(text);

        Assert.True(result.IsSupported);
        Assert.Equal(
            CalculationTaskType.OptimizationFrequency,
            result.TaskType);
        Assert.Equal(
            "optimization-frequency",
            result.WorkflowId);
    }

    /// <summary>验证只有优化意图时仍然使用普通优化任务。</summary>
    [Theory]
    [InlineData("优化")]
    [InlineData("opt")]
    public async Task InterpretsPlainOptimizationTask(
        string text)
    {
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask result =
            await interpreter.InterpretAsync(text);

        Assert.True(result.IsSupported);
        Assert.Equal(
            CalculationTaskType.Optimization,
            result.TaskType);
        Assert.Equal(
            "optimization-complete",
            result.WorkflowId);
    }

    /// <summary>验证联合任务会写入优化默认参数。</summary>
    [Fact]
    public void ResolverAppliesOptimizationDefaults()
    {
        CalculationSpec spec =
            CalculationDefaults
                .CreateDefaultOptimizationFrequency();
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType =
            CalculationTaskType.OptimizationFrequency;
        context.ElectronCount = 10;

        CalculationParameterResolutionResult result =
            new CalculationParameterResolver().Resolve(
                spec,
                new List<CalculationParameterIntent>(),
                context);

        Assert.Equal(
            CalculationParameterResolutionStatus.Resolved,
            result.Status);
        Assert.Equal(
            "100",
            result.Spec.ExtraOptions[
                CalculationOptionKeys.OptimizationMaxCycles]);
    }

    /// <summary>验证联合 Workflow 在计算节点后执行稳定性检查。</summary>
    [Fact]
    public void WorkflowContainsStabilityCheckNode()
    {
        WorkflowDefinition definition =
            OptimizationFrequencyWorkflowDefinitionFactory.Create(
                "workflow-opt-freq",
                "联合优化频率");

        Assert.Equal(6, definition.Nodes.Count);
        WorkflowNode calculation = definition.Nodes[0];
        WorkflowNode stability = definition.Nodes[1];
        WorkflowNode correctionPlan = definition.Nodes[2];
        WorkflowNode recoveryJob = definition.Nodes[3];
        WorkflowNode recoveryExecution = definition.Nodes[4];
        WorkflowNode recoveryRecheck = definition.Nodes[5];

        Assert.Equal(
            CalculationSkillIds.CalculationOptimizationFrequency,
            calculation.Skill);
        Assert.Equal(
            "optimization-frequency-stability-check",
            stability.Id);
        Assert.Contains(calculation.Id, stability.DependsOn);
        Assert.Equal(
            calculation.Id,
            stability.Inputs["optimizationFrequency"]);
        Assert.Equal(
            "optimization-frequency-stability-correction-plan",
            correctionPlan.Id);
        Assert.Contains(
            "wavefunction-instability",
            correctionPlan.ConditionAnyContains);
        Assert.Equal(
            correctionPlan.Id,
            recoveryJob.ConditionInput);
        Assert.Contains(
            recoveryJob.Id,
            recoveryExecution.DependsOn);
        Assert.Contains(
            recoveryExecution.Id,
            recoveryRecheck.DependsOn);
        Assert.Equal(
            calculation.Id,
            recoveryRecheck.Inputs["optimizationFrequency"]);
    }
}
