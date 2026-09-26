using System.Text.Json;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>
/// 基于 ISkillRegistry 的通用 JSON 技能调用器。
/// </summary>
public sealed class SkillJsonInvoker : ISkillInvoker
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly ISkillRegistry _registry;

    /// <summary>创建技能调用器。</summary>
    public SkillJsonInvoker(ISkillRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        _registry = registry;
    }

    /// <summary>调用指定技能。</summary>
    public async Task<TResult> InvokeAsync<TRequest, TResult>(
        string skillId,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResult : class
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        ISkill? skill = _registry.Resolve(skillId);
        if (skill == null)
        {
            throw new InvalidOperationException("没有注册技能：" + skillId);
        }

        TaskRequest taskRequest = new TaskRequest();
        taskRequest.SkillId = skillId;
        taskRequest.Inputs["request"] =
            JsonSerializer.Serialize(request, JsonOptions);

        TaskResult taskResult =
            await skill.ExecuteAsync(taskRequest, cancellationToken);

        if (!taskResult.Succeeded)
        {
            throw new InvalidOperationException(
                taskResult.Diagnostics ?? "技能执行失败：" + skillId);
        }

        if (string.IsNullOrWhiteSpace(taskResult.Output))
        {
            throw new InvalidOperationException("技能没有返回结果：" + skillId);
        }

        TResult? typedResult =
            JsonSerializer.Deserialize<TResult>(taskResult.Output, JsonOptions);

        if (typedResult == null)
        {
            throw new InvalidOperationException("无法反序列化技能结果：" + skillId);
        }

        return typedResult;
    }
}
