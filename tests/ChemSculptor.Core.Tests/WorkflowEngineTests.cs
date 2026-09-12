using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 工作流内核行为测试。
/// </summary>
public class WorkflowEngineTests
{
    /// <summary>验证节点按依赖顺序执行并最终通过。</summary>
    [Fact]
    public async Task RunsNodesInDependencyOrderAndPasses()
    {
        RecordingContainer recorder = new RecordingContainer();
        WorkflowEngine engine = CreateEngine(recorder);

        WorkflowDefinition definition = new WorkflowDefinition();
        definition.Id = "wf_order";
        definition.Version = "1.0.0";
        definition.Goal = "dependency order demo";
        definition.Nodes = new List<WorkflowNode>();

        WorkflowNode nodeA = new WorkflowNode();
        nodeA.Id = "a";
        nodeA.Container = recorder.Name;

        WorkflowNode nodeB = new WorkflowNode();
        nodeB.Id = "b";
        nodeB.Container = recorder.Name;
        nodeB.DependsOn = new List<string>();
        nodeB.DependsOn.Add("a");

        WorkflowNode nodeC = new WorkflowNode();
        nodeC.Id = "c";
        nodeC.Container = recorder.Name;
        nodeC.DependsOn = new List<string>();
        nodeC.DependsOn.Add("a");
        nodeC.DependsOn.Add("b");

        definition.Nodes.Add(nodeA);
        definition.Nodes.Add(nodeB);
        definition.Nodes.Add(nodeC);

        WorkflowRun submitted = await engine.SubmitAsync(definition);
        Assert.Equal(WorkflowState.Ready, submitted.State);

        WorkflowRun run = await engine.RunAsync(submitted.Id);

        Assert.Equal(WorkflowState.Passed, run.State);

        foreach (KeyValuePair<string, TaskState> pair in run.NodeStates)
        {
            Assert.Equal(TaskState.Passed, pair.Value);
        }

        Assert.Equal(3, recorder.Order.Count);
        Assert.Equal("a", recorder.Order[0]);
        Assert.Equal("b", recorder.Order[1]);
        Assert.Equal("c", recorder.Order[2]);
    }

    /// <summary>验证验证门拒绝时工作流进入失败状态。</summary>
    [Fact]
    public async Task FailsWorkflowWhenValidationGateRejects()
    {
        WorkflowEngine engine = CreateEngine(new EchoSkillContainer(), new RejectingValidationGate());

        WorkflowDefinition definition = new WorkflowDefinition();
        definition.Id = "wf_gate";
        definition.Version = "1.0.0";
        definition.Goal = "validation gate demo";
        definition.Nodes = new List<WorkflowNode>();

        WorkflowNode node = new WorkflowNode();
        node.Id = "soc";
        node.Container = "echo";
        node.Gate = "validate_soc_quality";
        definition.Nodes.Add(node);

        WorkflowRun submitted = await engine.SubmitAsync(definition);
        WorkflowRun run = await engine.RunAsync(submitted.Id);

        Assert.Equal(WorkflowState.Failed, run.State);
        Assert.Equal(TaskState.Failed, run.NodeStates["soc"]);
        Assert.Contains("Validation gate", run.Results["soc"].Diagnostics);
    }

    /// <summary>创建用于测试的工作流引擎。</summary>
    private static WorkflowEngine CreateEngine(
        ISkillContainer? container = null,
        IValidationGate? gate = null)
    {
        ISkillContainer actualContainer;
        if (container == null)
        {
            actualContainer = new EchoSkillContainer();
        }
        else
        {
            actualContainer = container;
        }

        IValidationGate actualGate;
        if (gate == null)
        {
            actualGate = new PassThroughValidationGate();
        }
        else
        {
            actualGate = gate;
        }

        ContainerRegistry registry = new ContainerRegistry();
        registry.RegisterAsync(actualContainer).GetAwaiter().GetResult();

        return new WorkflowEngine(
            registry,
            new InMemoryEventBus(),
            new InMemoryWorkflowRepository(),
            new AllowAllRuleEngine(),
            actualGate,
            new InMemoryCaseMemory());
    }

    /// <summary>记录节点执行顺序的测试容器。</summary>
    private sealed class RecordingContainer : ISkillContainer
    {
        private readonly List<string> _order;
        private readonly List<string> _capabilities;

        public RecordingContainer()
        {
            _order = new List<string>();
            _capabilities = new List<string>();
            _capabilities.Add("record");
        }

        public List<string> Order
        {
            get { return _order; }
        }

        public string Name
        {
            get { return "recorder"; }
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
            _order.Add(request.NodeId);

            TaskResult result = new TaskResult();
            result.WorkflowId = request.WorkflowId;
            result.NodeId = request.NodeId;
            result.Succeeded = true;
            result.Output = request.NodeId;

            return Task.FromResult(result);
        }

        public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    /// <summary>始终拒绝结果的测试验证门。</summary>
    private sealed class RejectingValidationGate : IValidationGate
    {
        public Task<ValidationReport> ValidateAsync(
            TaskResult result,
            CancellationToken cancellationToken = default)
        {
            ValidationReport report = new ValidationReport();
            report.Status = "Failed";
            report.Confidence = 0.1;

            return Task.FromResult(report);
        }
    }
}
