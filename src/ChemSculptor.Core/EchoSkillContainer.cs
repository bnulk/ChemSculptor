using System.Text;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public sealed class EchoSkillContainer : ISkillContainer
{
    private readonly List<string> _capabilities;

    public EchoSkillContainer()
    {
        _capabilities = new List<string>();
        _capabilities.Add("demo");
        _capabilities.Add("passthrough");
    }

    public string Name
    {
        get { return "echo"; }
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
        StringBuilder inputBuilder = new StringBuilder();

        if (request.Inputs.Count == 0)
        {
            inputBuilder.Append("no upstream outputs");
        }
        else
        {
            bool first = true;

            foreach (KeyValuePair<string, string> pair in request.Inputs)
            {
                if (!first)
                {
                    inputBuilder.Append(", ");
                }

                inputBuilder.Append(pair.Key);
                inputBuilder.Append("=");
                inputBuilder.Append(pair.Value);
                first = false;
            }
        }

        TaskResult result = new TaskResult();
        result.WorkflowId = request.WorkflowId;
        result.NodeId = request.NodeId;
        result.Succeeded = true;
        result.Output = "[" + request.ContainerId + "] ok (" + inputBuilder.ToString() + ")";

        return Task.FromResult(result);
    }

    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
