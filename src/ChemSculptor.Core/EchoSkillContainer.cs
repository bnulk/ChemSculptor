using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class EchoSkillContainer : ISkillContainer
{
    public string Name => "echo";

    public string Version => "1.0.0";

    public IReadOnlyList<string> Capabilities { get; } = ["demo", "passthrough"];

    public Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        var inputs = request.Inputs.Count == 0
            ? "no upstream outputs"
            : string.Join(", ", request.Inputs.Select(pair => $"{pair.Key}={pair.Value}"));

        return Task.FromResult(new TaskResult
        {
            WorkflowId = request.WorkflowId,
            NodeId = request.NodeId,
            Succeeded = true,
            Output = $"[{request.ContainerId}] ok ({inputs})"
        });
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
