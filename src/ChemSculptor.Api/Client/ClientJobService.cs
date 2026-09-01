using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using ChemSculptor.Core;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor;

namespace ChemSculptor.Api.Client;

public sealed class ClientJobService
{
    private readonly ConcurrentDictionary<string, ClientJob> _jobs =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, WorkflowDefinition> _templates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly IClientInputParser _parser;
    private readonly WorkflowEngine _engine;

    public ClientJobService(IClientInputParser parser, WorkflowEngine engine)
    {
        _parser = parser;
        _engine = engine;
        LoadTemplates();
    }

    public async Task<ClientJob> SubmitAsync(Stream input, CancellationToken cancellationToken)
    {
        var rawText = await ReadAllTextAsync(input, cancellationToken);
        var job = new ClientJob
        {
            Id = $"job-{Guid.NewGuid():N}"
        };
        _jobs[job.Id] = job;

        _ = Task.Run(async () => await ExecuteAsync(job, rawText), cancellationToken);
        return job;
    }

    public ClientJob? GetJob(string jobId) =>
        _jobs.TryGetValue(jobId, out var job) ? job : null;

    private async Task ExecuteAsync(ClientJob job, string rawText)
    {
        try
        {
            var request = await _parser.ParseAsync(rawText);
            job.Status = "Running";
            job.StartedAt = DateTimeOffset.UtcNow;
            job.Message = $"已解析输入，工作流：{request.WorkflowId}";

            var definition = BuildDefinition(job.Id, request);
            var submitted = await _engine.SubmitAsync(definition);
            var run = await _engine.RunAsync(submitted.Id);

            job.Status = run.State == WorkflowState.Passed ? "Passed" : "Failed";
            job.Message = $"工作流结束：{run.State}";
            job.ResultText = BuildResultText(job, rawText, run);
        }
        catch (Exception ex)
        {
            job.Status = "Failed";
            job.Message = ex.Message;
            job.ResultText = $"ChemSculptor Job: {job.Id}\nStatus: Failed\n错误: {ex.Message}";
        }
        finally
        {
            job.CompletedAt = DateTimeOffset.UtcNow;
        }
    }

    private WorkflowDefinition BuildDefinition(string jobId, ProcessedClientRequest request)
    {
        if (_templates.TryGetValue(request.WorkflowId, out var template))
        {
            return template with { Id = jobId, Goal = request.Goal };
        }

        return new WorkflowDefinition
        {
            Id = jobId,
            Version = "1.0.0",
            Goal = request.Goal,
            Nodes =
            [
                new WorkflowNode
                {
                    Id = "client_task",
                    Container = "echo"
                }
            ]
        };
    }

    private static string BuildResultText(ClientJob job, string rawText, WorkflowRun run)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"ChemSculptor Job: {job.Id}");
        builder.AppendLine($"Status: {run.State}");
        builder.AppendLine($"Goal: {run.Definition.Goal}");
        builder.AppendLine();
        builder.AppendLine("--- 原始请求 ---");
        builder.AppendLine(rawText.TrimEnd());
        builder.AppendLine("--- 节点输出 ---");

        foreach (var (nodeId, result) in run.Results.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"[{nodeId}] {(result.Succeeded ? "Passed" : "Failed")}");

            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                builder.AppendLine(result.Output);
            }

            if (!string.IsNullOrWhiteSpace(result.Diagnostics))
            {
                builder.AppendLine(result.Diagnostics);
            }
        }

        builder.AppendLine("--- 结束 ---");
        return builder.ToString();
    }

    private static async Task<string> ReadAllTextAsync(Stream input, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(input);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private void LoadTemplates()
    {
        var workflowsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workflows");
        if (!Directory.Exists(workflowsDirectory))
        {
            return;
        }

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        foreach (var file in Directory.EnumerateFiles(workflowsDirectory, "*.json"))
        {
            var json = File.ReadAllText(file);
            var definition = JsonSerializer.Deserialize<WorkflowDefinition>(json, options);
            if (definition is not null)
            {
                _templates[definition.Id] = definition;
            }
        }
    }
}
