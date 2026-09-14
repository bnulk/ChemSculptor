using System.Text;
using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 演示用技能。
/// 不执行任何真实计算，只把上游输出拼接后返回，用于验证整条链路。
/// </summary>
public sealed class EchoSkill : ISkill
{
    private readonly List<string> _capabilities;

    public EchoSkill()
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

    /// <summary>执行演示任务并返回透传结果。</summary>
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
        result.Output = "[" + request.SkillId + "] ok (" + inputBuilder.ToString() + ")";

        return Task.FromResult(result);
    }

    /// <summary>演示技能始终报告健康。</summary>
    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
