using ChemSculptor.Agent;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.Core.Tests;

/// <summary>优化完整工作流定义和分支测试。</summary>
public class OptimizationWorkflowDefinitionFactoryTests
{
    /// <summary>验证工作流包含完整节点和条件分支。</summary>
    [Fact]
    public void ContainsCompleteOptimizationWorkflow()
    {
        WorkflowDefinition definition =
            OptimizationWorkflowDefinitionFactory.Create(
                "workflow-optimization",
                "优化完整工作流");

        Assert.NotNull(FindNode(definition, "optimization"));
        Assert.NotNull(
            FindNode(definition, "optimization-validation"));
        Assert.NotNull(
            FindNode(definition, "optimization-anomaly"));
        Assert.NotNull(
            FindNode(definition, "optimization-diagnosis"));
        Assert.NotNull(
            FindNode(definition, "scf-repair-branch"));
        Assert.NotNull(
            FindNode(definition, "optimization-repair-branch"));
        Assert.NotNull(
            FindNode(definition, "recovery-branch-selection"));
        Assert.NotNull(
            FindNode(definition, "optimization-recovery-job"));
        Assert.NotNull(
            FindNode(definition, "optimization-recovery-execution"));
        Assert.NotNull(
            FindNode(definition, "optimization-recovery-validation"));
        Assert.NotNull(
            FindNode(definition, "optimization-processing-plan"));
        Assert.NotNull(
            FindNode(definition, "optimization-science-data"));

        WorkflowNode scfBranch =
            FindNode(definition, "scf-repair-branch")!;
        WorkflowNode optimizationBranch =
            FindNode(definition, "optimization-repair-branch")!;

        Assert.Contains(
            CommonAnomalyCodes.ScfNotConverged,
            scfBranch.ConditionAnyContains);
        Assert.Contains(
            OptimizationAnomalyCodes.NotConverged,
            optimizationBranch.ConditionAnyContains);
    }

    /// <summary>验证正常、SCF 异常和优化未收敛三条路径。</summary>
    [Theory]
    [InlineData("normal")]
    [InlineData("scf")]
    [InlineData("optimization")]
    public async Task RunsNormalScfAndOptimizationPaths(
        string path)
    {
        WorkflowDefinition definition =
            OptimizationWorkflowDefinitionFactory.Create(
                "workflow-" + path,
                path);
        WorkflowEngine engine = CreateEngine(path);
        WorkflowRun submitted =
            await engine.SubmitAsync(
                definition,
                CreateInputs());
        WorkflowRun run =
            await engine.RunAsync(submitted.Id);

        Assert.True(
            run.State == WorkflowState.Passed,
            DescribeRun(run));
        Assert.Equal(
            TaskState.Passed,
            run.NodeStates["optimization"]);
        Assert.Equal(
            TaskState.Passed,
            run.NodeStates["optimization-processing-plan"]);
        Assert.Equal(
            TaskState.Passed,
            run.NodeStates["optimization-science-data"]);

        if (path == "normal")
        {
            Assert.Equal(
                TaskState.Skipped,
                run.NodeStates["scf-repair-branch"]);
            Assert.Equal(
                TaskState.Skipped,
                run.NodeStates["optimization-repair-branch"]);
            Assert.Equal(
                TaskState.Skipped,
                run.NodeStates["optimization-recovery-execution"]);
        }
        else if (path == "scf")
        {
            Assert.Equal(
                TaskState.Passed,
                run.NodeStates["scf-repair-branch"]);
            Assert.Equal(
                TaskState.Skipped,
                run.NodeStates["optimization-repair-branch"]);
            Assert.Equal(
                TaskState.Passed,
                run.NodeStates["optimization-recovery-execution"]);
        }
        else
        {
            Assert.Equal(
                TaskState.Skipped,
                run.NodeStates["scf-repair-branch"]);
            Assert.Equal(
                TaskState.Passed,
                run.NodeStates["optimization-repair-branch"]);
            Assert.Equal(
                TaskState.Passed,
                run.NodeStates["optimization-recovery-execution"]);
        }
    }

    /// <summary>验证程序硬失败会中止工作流。</summary>
    [Fact]
    public async Task AbortsOnProgramHardFailure()
    {
        WorkflowDefinition definition =
            OptimizationWorkflowDefinitionFactory.Create(
                "workflow-hard-failure",
                "程序硬失败");
        WorkflowEngine engine =
            CreateEngine("hard-failure");
        WorkflowRun submitted =
            await engine.SubmitAsync(
                definition,
                CreateInputs());
        WorkflowRun run =
            await engine.RunAsync(submitted.Id);

        Assert.True(
            run.State == WorkflowState.Failed,
            DescribeRun(run));
        Assert.True(
            run.NodeStates["optimization-hard-failure"]
                == TaskState.Failed,
            DescribeRun(run));
    }

    private static WorkflowEngine CreateEngine(string path)
    {
        SkillRegistry registry = new SkillRegistry();
        Register(
            registry,
            CalculationSkillIds.CalculationOptimization,
            path);
        Register(
            registry,
            CalculationSkillIds.CalculationOptimizationValidation,
            path);
        Register(
            registry,
            AnomalySkillIds.CheckOptimizationConvergence,
            path);
        Register(
            registry,
            AnomalySkillIds.DiagnoseOptimizationFailure,
            path);
        Register(
            registry,
            AnomalySkillIds.PlanOptimizationCorrection,
            path);
        Register(
            registry,
            AnomalySkillIds.SelectOptimizationRecoveryBranch,
            path);
        Register(
            registry,
            AnomalySkillIds.CreateOptimizationRecoveryJob,
            path);
        Register(
            registry,
            AnomalySkillIds.ExecuteOptimizationRecoveryJob,
            path);
        Register(
            registry,
            AnomalySkillIds.ValidateOptimizationRecovery,
            path);
        Register(
            registry,
            AnomalySkillIds.ReportOptimizationHardFailure,
            path);
        Register(
            registry,
            CalculationSkillIds.CalculationOptimizationProcessingPlan,
            path);
        Register(
            registry,
            ScientificDataSkillIds.RecordOptimizationResult,
            path);

        return new WorkflowEngine(
            registry,
            new InMemoryEventBus(),
            new InMemoryWorkflowRepository(),
            new AllowAllRuleEngine(),
            new PassThroughValidationGate(),
            new InMemoryCaseMemory());
    }

    private static Dictionary<string, string> CreateInputs()
    {
        Dictionary<string, string> inputs =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        inputs["request"] = "{}";
        return inputs;
    }

    private static void Register(
        SkillRegistry registry,
        string skillId,
        string path)
    {
        registry.RegisterAsync(
            new ScriptedWorkflowSkill(skillId, path))
            .GetAwaiter()
            .GetResult();
    }

    private static WorkflowNode? FindNode(
        WorkflowDefinition definition,
        string nodeId)
    {
        for (int index = 0;
            index < definition.Nodes.Count;
            index++)
        {
            if (string.Equals(
                definition.Nodes[index].Id,
                nodeId,
                StringComparison.OrdinalIgnoreCase))
            {
                return definition.Nodes[index];
            }
        }

        return null;
    }

    private static string DescribeRun(WorkflowRun run)
    {
        string text = "State=" + run.State.ToString();

        foreach (KeyValuePair<string, TaskState> pair in
            run.NodeStates)
        {
            text = text +
                "; " +
                pair.Key +
                "=" +
                pair.Value.ToString();
        }

        foreach (KeyValuePair<string, TaskResult> pair in
            run.Results)
        {
            if (!pair.Value.Succeeded)
            {
                text = text +
                    "; error[" +
                    pair.Key +
                    "]=" +
                    pair.Value.Diagnostics;
            }
        }

        return text;
    }

    private sealed class ScriptedWorkflowSkill : ISkill
    {
        private readonly string _path;
        private readonly List<string> _capabilities;

        public ScriptedWorkflowSkill(
            string skillId,
            string path)
        {
            NameValue = skillId;
            _path = path;
            _capabilities = new List<string>();
            _capabilities.Add("test.workflow-skill");
        }

        public string NameValue { get; private set; }

        public string Name
        {
            get { return NameValue; }
        }

        public string Version
        {
            get { return "1.0.0"; }
        }

        public IReadOnlyList<string> Capabilities
        {
            get { return _capabilities; }
        }

        public Task<TaskResult> ExecuteAsync(
            TaskRequest request,
            CancellationToken cancellationToken = default)
        {
            if (Name
                == AnomalySkillIds.ReportOptimizationHardFailure
                && _path == "hard-failure")
            {
                TaskResult failed = new TaskResult();
                failed.WorkflowId = request.WorkflowId;
                failed.NodeId = request.NodeId;
                failed.Succeeded = false;
                failed.Diagnostics = "程序硬失败";
                return Task.FromResult(failed);
            }

            TaskResult result = new TaskResult();
            result.WorkflowId = request.WorkflowId;
            result.NodeId = request.NodeId;
            result.Succeeded = true;
            result.Output = CreateOutput();
            return Task.FromResult(result);
        }

        public Task<bool> HealthAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        private string CreateOutput()
        {
            if (Name == AnomalySkillIds.CheckOptimizationConvergence)
            {
                if (_path == "scf")
                {
                    return "{\"summary\":\"scf.not_converged\"}";
                }

                if (_path == "optimization")
                {
                    return "{\"summary\":\"optimization.not_converged\"}";
                }

                if (_path == "hard-failure")
                {
                    return "{\"summary\":\"output.missing\"}";
                }

                return "{\"summary\":\"passed\"}";
            }

            if (Name == AnomalySkillIds.PlanOptimizationCorrection
                || Name
                    == AnomalySkillIds
                        .SelectOptimizationRecoveryBranch)
            {
                return "{\"planCreated\":true,\"plan\":{}}";
            }

            return "{}";
        }
    }
}
