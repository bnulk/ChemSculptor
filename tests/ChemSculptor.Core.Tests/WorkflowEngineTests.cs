using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

public class WorkflowEngineTests
{
    [Fact]
    public async Task RunsNodesInDependencyOrderAndPasses()
    {
        var recorder = new RecordingContainer();
        var engine = CreateEngine(recorder);
        var definition = new WorkflowDefinition
        {
            Id = "wf_order",
            Version = "1.0.0",
            Goal = "dependency order demo",
            Nodes =
            [
                new WorkflowNode { Id = "a", Container = recorder.Name },
                new WorkflowNode { Id = "b", Container = recorder.Name, DependsOn = ["a"] },
                new WorkflowNode { Id = "c", Container = recorder.Name, DependsOn = ["a", "b"] }
            ]
        };

        var submitted = await engine.SubmitAsync(definition);
        Assert.Equal(WorkflowState.Ready, submitted.State);

        var run = await engine.RunAsync(submitted.Id);

        Assert.Equal(WorkflowState.Passed, run.State);
        Assert.All(run.NodeStates, pair => Assert.Equal(TaskState.Passed, pair.Value));
        Assert.Equal(["a", "b", "c"], recorder.Order);
    }

    [Fact]
    public async Task FailsWorkflowWhenValidationGateRejects()
    {
        var engine = CreateEngine(
            new EchoSkillContainer(),
            new RejectingValidationGate());
        var definition = new WorkflowDefinition
        {
            Id = "wf_gate",
            Version = "1.0.0",
            Goal = "validation gate demo",
            Nodes =
            [
                new WorkflowNode
                {
                    Id = "soc",
                    Container = "echo",
                    Gate = "validate_soc_quality"
                }
            ]
        };

        var submitted = await engine.SubmitAsync(definition);
        var run = await engine.RunAsync(submitted.Id);

        Assert.Equal(WorkflowState.Failed, run.State);
        Assert.Equal(TaskState.Failed, run.NodeStates["soc"]);
        Assert.Contains("Validation gate", run.Results["soc"].Diagnostics);
    }

    private static WorkflowEngine CreateEngine(
        ISkillContainer? container = null,
        IValidationGate? gate = null)
    {
        var registry = new ContainerRegistry();
        registry.RegisterAsync(container ?? new EchoSkillContainer()).GetAwaiter().GetResult();

        return new WorkflowEngine(
            registry,
            new InMemoryEventBus(),
            new InMemoryWorkflowRepository(),
            new AllowAllRuleEngine(),
            gate ?? new PassThroughValidationGate(),
            new InMemoryCaseMemory());
    }

    private sealed class RecordingContainer : ISkillContainer
    {
        public List<string> Order { get; } = [];

        public string Name => "recorder";

        public string Version => "1.0.0";

        public IReadOnlyList<string> Capabilities { get; } = ["record"];

        public Task<TaskResult> ExecuteAsync(
            TaskRequest request,
            CancellationToken cancellationToken = default)
        {
            Order.Add(request.NodeId);
            return Task.FromResult(new TaskResult
            {
                WorkflowId = request.WorkflowId,
                NodeId = request.NodeId,
                Succeeded = true,
                Output = request.NodeId
            });
        }

        public Task<bool> HealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class RejectingValidationGate : IValidationGate
    {
        public Task<ValidationReport> ValidateAsync(
            TaskResult result,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ValidationReport
            {
                Status = "Failed",
                Confidence = 0.1
            });
    }
}
