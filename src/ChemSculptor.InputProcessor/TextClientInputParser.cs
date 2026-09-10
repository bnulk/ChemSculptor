namespace ChemSculptor.InputProcessor;

public sealed class TextClientInputParser : IClientInputParser
{
    public const string DefaultWorkflowId = "tadf_mechanism_diagnosis";

    public Task<ProcessedClientRequest> ParseAsync(
        string rawText,
        CancellationToken cancellationToken = default)
    {
        string workflowId = DefaultWorkflowId;
        string goal = "客户端文本任务";
        List<string> diagnostics = new List<string>();

        char[] separators = new char[1];
        separators[0] = '\n';
        string[] lines = rawText.Split(separators, StringSplitOptions.RemoveEmptyEntries);

        for (int index = 0; index < lines.Length; index++)
        {
            string line = lines[index].Trim();
            string workflowValue;
            string goalValue;

            if (TryReadValue(line, "workflow:", out workflowValue))
            {
                workflowId = workflowValue;
            }
            else if (TryReadValue(line, "goal:", out goalValue))
            {
                goal = goalValue;
            }
        }

        if (string.Equals(workflowId, DefaultWorkflowId, StringComparison.OrdinalIgnoreCase))
        {
            diagnostics.Add("未指定 workflow，使用默认工作流 " + DefaultWorkflowId);
        }

        ProcessedClientRequest request = new ProcessedClientRequest();
        request.WorkflowId = workflowId;
        request.Goal = goal;
        request.RawText = rawText;
        request.Diagnostics = diagnostics;

        return Task.FromResult(request);
    }

    private static bool TryReadValue(string line, string prefix, out string value)
    {
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = string.Empty;
            return false;
        }

        value = line.Substring(prefix.Length).Trim();
        return true;
    }
}
