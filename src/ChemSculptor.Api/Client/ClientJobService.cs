using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;

namespace ChemSculptor.Api.Client;

/// <summary>
/// 客户端任务服务。
/// 负责接收文本任务、后台执行工作流，并提供状态与结果查询。
/// </summary>
public sealed class ClientJobService
{
    private readonly ConcurrentDictionary<string, ClientJob> _jobs =
        new ConcurrentDictionary<string, ClientJob>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, WorkflowDefinition> _templates =
        new Dictionary<string, WorkflowDefinition>(StringComparer.OrdinalIgnoreCase);

    private readonly IClientInputParser _parser;
    private readonly WorkflowEngine _engine;

    public ClientJobService(IClientInputParser parser, WorkflowEngine engine)
    {
        _parser = parser;
        _engine = engine;
        LoadTemplates();
    }

    /// <summary>创建任务并在后台开始执行。</summary>
    public Task<ClientJob> SubmitAsync(string rawText, CancellationToken cancellationToken)
    {
        ClientJob job = new ClientJob();
        job.Id = "job-" + Guid.NewGuid().ToString("N");
        _jobs[job.Id] = job;

        // 立即返回任务编号，实际执行在后台异步进行。
        Task executionTask = ExecuteAsync(job, rawText);
        return Task.FromResult(job);
    }

    /// <summary>按任务编号查询任务；不存在时返回 null。</summary>
    public ClientJob? GetJob(string jobId)
    {
        ClientJob? job;
        if (_jobs.TryGetValue(jobId, out job))
        {
            return job;
        }

        return null;
    }

    /// <summary>执行任务：解析输入、执行工作流并生成结果文本。</summary>
    private async Task ExecuteAsync(ClientJob job, string rawText)
    {
        try
        {
            ProcessedClientRequest request = await _parser.ParseAsync(rawText);
            job.Status = "Running";
            job.StartedAt = DateTimeOffset.UtcNow;
            job.Message = "已解析输入，工作流：" + request.WorkflowId;

            WorkflowDefinition definition = BuildDefinition(job.Id, request);
            WorkflowRun submitted = await _engine.SubmitAsync(definition);
            WorkflowRun run = await _engine.RunAsync(submitted.Id);

            if (run.State == WorkflowState.Passed)
            {
                job.Status = "Passed";
            }
            else
            {
                job.Status = "Failed";
            }

            job.Message = "工作流结束：" + run.State.ToString();
            job.ResultText = BuildResultText(job, rawText, run);
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.Message = ex.Message;
            job.ResultText = "ChemSculptor Job: " + job.Id +
                Environment.NewLine + "Status: Failed" +
                Environment.NewLine + "错误: " + ex.Message;
        }
        finally
        {
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>根据解析结果选择模板或构建兜底工作流。</summary>
    private WorkflowDefinition BuildDefinition(string jobId, ProcessedClientRequest request)
    {
        WorkflowDefinition? template;

        if (_templates.TryGetValue(request.WorkflowId, out template))
        {
            return CloneDefinition(template, jobId, request.Goal);
        }

        WorkflowDefinition fallback = new WorkflowDefinition();
        fallback.Id = jobId;
        fallback.Version = "1.0.0";
        fallback.Goal = request.Goal;
        fallback.Nodes = new List<WorkflowNode>();

        WorkflowNode node = new WorkflowNode();
        node.Id = "client_task";
        node.Container = "echo";
        fallback.Nodes.Add(node);

        return fallback;
    }

    /// <summary>复制模板并替换任务标识与目标描述。</summary>
    private static WorkflowDefinition CloneDefinition(
        WorkflowDefinition template,
        string jobId,
        string goal)
    {
        WorkflowDefinition clone = new WorkflowDefinition();
        clone.Id = jobId;
        clone.Version = template.Version;
        clone.Goal = goal;
        clone.Nodes = new List<WorkflowNode>();

        for (int index = 0; index < template.Nodes.Count; index++)
        {
            WorkflowNode sourceNode = template.Nodes[index];
            WorkflowNode targetNode = new WorkflowNode();
            targetNode.Id = sourceNode.Id;
            targetNode.Container = sourceNode.Container;
            targetNode.DependsOn = new List<string>(sourceNode.DependsOn);
            targetNode.Gate = sourceNode.Gate;
            clone.Nodes.Add(targetNode);
        }

        return clone;
    }

    /// <summary>生成便于阅读的结果文本。</summary>
    private static string BuildResultText(ClientJob job, string rawText, WorkflowRun run)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("ChemSculptor Job: " + job.Id);
        builder.AppendLine("Status: " + run.State.ToString());
        builder.AppendLine("Goal: " + run.Definition.Goal);
        builder.AppendLine();
        builder.AppendLine("--- 原始请求 ---");
        builder.AppendLine(rawText.TrimEnd());
        builder.AppendLine("--- 节点输出 ---");

        List<KeyValuePair<string, TaskResult>> results =
            new List<KeyValuePair<string, TaskResult>>(run.Results);
        results.Sort(CompareResults);

        for (int index = 0; index < results.Count; index++)
        {
            KeyValuePair<string, TaskResult> pair = results[index];
            string stateText = "Failed";

            if (pair.Value.Succeeded)
            {
                stateText = "Passed";
            }

            builder.AppendLine("[" + pair.Key + "] " + stateText);

            if (!string.IsNullOrWhiteSpace(pair.Value.Output))
            {
                builder.AppendLine(pair.Value.Output);
            }

            if (!string.IsNullOrWhiteSpace(pair.Value.Diagnostics))
            {
                builder.AppendLine(pair.Value.Diagnostics);
            }
        }

        builder.AppendLine("--- 结束 ---");
        return builder.ToString();
    }

    /// <summary>按节点标识排序结果，便于稳定输出。</summary>
    private static int CompareResults(
        KeyValuePair<string, TaskResult> left,
        KeyValuePair<string, TaskResult> right)
    {
        return string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>从 workflows 目录载入所有工作流模板。</summary>
    private void LoadTemplates()
    {
        string workflowsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workflows");
        if (!Directory.Exists(workflowsDirectory))
        {
            return;
        }

        JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        foreach (string file in Directory.EnumerateFiles(workflowsDirectory, "*.json"))
        {
            string json = File.ReadAllText(file);
            WorkflowDefinition? definition = JsonSerializer.Deserialize<WorkflowDefinition>(json, options);

            if (definition != null)
            {
                _templates[definition.Id] = definition;
            }
        }
    }
}
