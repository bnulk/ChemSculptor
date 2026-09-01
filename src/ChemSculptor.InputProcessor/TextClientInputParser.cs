namespace ChemSculptor.InputProcessor;

public sealed class TextClientInputParser : IClientInputParser
{
    public const string DefaultWorkflowId = "tadf_mechanism_diagnosis";

    public Task<ProcessedClientRequest> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        var workflowId = DefaultWorkflowId;
        var goal = "客户端文本任务";
        var diagnostics = new List<string>();

        foreach (var rawLine in rawText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            if (TryReadValue(line, "workflow:", out var workflowValue))
            {
                workflowId = workflowValue;
            }
            else if (TryReadValue(line, "goal:", out var goalValue))
            {
                goal = goalValue;
            }
        }

        if (string.Equals(workflowId, DefaultWorkflowId, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add($"未指定 workflow，使用默认工作流 {DefaultWorkflowId}");
        }

        return Task.FromResult(new ProcessedClientRequest
        {
            WorkflowId = workflowId,
            Goal = goal,
            RawText = rawText,
            Diagnostics = diagnostics
        });
    }

    private static bool TryReadValue(string line, string prefix, out string value)
    {
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = string.Empty;
            return false;
        }

        value = line[prefix.Length..].Trim();
        return true;
    }
}
