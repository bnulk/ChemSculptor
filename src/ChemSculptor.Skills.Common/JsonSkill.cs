using System.Text.Json;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common;

/// <summary>
/// 基于 JSON 请求和结果的技能基类。
/// 统一把 TaskRequest 中的 request 字段转换为强类型请求。
/// </summary>
public abstract class JsonSkill<TRequest, TResult> : ISkill
    where TRequest : class
    where TResult : class
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>请求在 TaskRequest.Inputs 中使用的键。</summary>
    public const string RequestKey = "request";

    /// <summary>技能唯一名称。</summary>
    public abstract string Name { get; }

    /// <summary>技能版本。</summary>
    public abstract string Version { get; }

    /// <summary>技能能力标签。</summary>
    public abstract IReadOnlyList<string> Capabilities { get; }

    /// <summary>执行强类型技能逻辑。</summary>
    protected abstract Task<TResult> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken);

    /// <summary>检查技能是否可用。</summary>
    public abstract Task<bool> HealthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>把 TaskRequest 转换为强类型请求并执行。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string? requestJson;
        if (!request.Inputs.TryGetValue(RequestKey, out requestJson))
        {
            throw new ArgumentException(
                "技能请求中缺少 " + RequestKey + "。",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(requestJson))
        {
            throw new ArgumentException(
                "技能请求中的 " + RequestKey + " 不能为空。",
                nameof(request));
        }

        TRequest? typedRequest =
            JsonSerializer.Deserialize<TRequest>(requestJson, JsonOptions);

        if (typedRequest == null)
        {
            throw new InvalidOperationException("无法反序列化技能请求。");
        }

        TResult typedResult =
            await ExecuteAsync(typedRequest, cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = JsonSerializer.Serialize(typedResult, JsonOptions);
        return taskResult;
    }
}
